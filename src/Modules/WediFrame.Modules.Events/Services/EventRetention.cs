using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WediFrame.Modules.Events.Domain;
using WediFrame.Shared.Audit;
using WediFrame.Shared.Storage;

namespace WediFrame.Modules.Events.Services;

/// <summary>
/// The Events module's public contract for retention-driven status transitions.
/// The Retention module (M4) owns the *schedule* (a background worker), but the
/// mutation of an <see cref="Event"/> lives here — in the module that owns the
/// entity — so status rules stay in one place and Retention only references
/// Events (one-way, no cycle; ARCHITECTURE.md §2).
///
/// Phase 1: time-based status flips, NO deletion.
///   Active                 → UploadClosed   when today &gt; UploadEndsAt
///   Active / UploadClosed  → Expired        when today &gt; ExpiresAt
/// Guest access already treats anything other than Active/UploadClosed as
/// unavailable, so Expired means the gallery is no longer reachable.
///
/// Phase 2: after a grace period past ExpiresAt, the event's media is physically
/// erased and the event moves to Deleted. The media purge itself lives in the
/// Media module (IEventMediaPurge); this module only finds due events and
/// finalizes the event (delete cover, flip status, audit). The Retention worker
/// orchestrates the two — that keeps Events free of a dependency on Media.
/// </summary>
public interface IEventRetention
{
    /// <summary>
    /// Apply all due status transitions and audit each one. Idempotent: an
    /// already-correct event is skipped, so re-running is safe (a crash mid-sweep
    /// just leaves work for the next call). Returns how many events moved to each
    /// state, for logging.
    /// </summary>
    Task<RetentionSweepResult> SweepAsync(CancellationToken ct = default);

    /// <summary>
    /// Ids of Expired events whose grace period has fully elapsed
    /// (today &gt; ExpiresAt + <paramref name="graceDays"/>), i.e. ready for
    /// physical erasure. The caller purges each event's media, then calls
    /// <see cref="FinalizeDeletionAsync"/>.
    /// </summary>
    Task<IReadOnlyList<Guid>> FindDueForPurgeAsync(int graceDays, CancellationToken ct = default);

    /// <summary>
    /// Finalize a hard-delete: remove the cover photo from R2, flip the event to
    /// Deleted, and audit it. Call this ONLY after the event's media has been
    /// purged. Idempotent — an already-Deleted or missing event is a no-op. The
    /// media/export counts are recorded in the audit entry for the erasure trail.
    /// <paramref name="actorUserId"/> is the host for a manual delete, or null for
    /// the retention job; <paramref name="cause"/> selects the audit action.
    /// </summary>
    Task FinalizeDeletionAsync(
        Guid eventId, int mediaDeleted, int exportsDeleted,
        Guid? actorUserId, EventDeletionCause cause, CancellationToken ct = default);
}

/// <summary>Why an event was hard-deleted — drives the audit action/reason.</summary>
public enum EventDeletionCause
{
    /// <summary>Retention job, after the grace period past ExpiresAt.</summary>
    RetentionGrace = 0,

    /// <summary>Host asked to delete their own event (right to erasure).</summary>
    HostRequest = 1,
}

/// <summary>Counts from one retention sweep.</summary>
public sealed record RetentionSweepResult(int UploadClosed, int Expired)
{
    public int Total => UploadClosed + Expired;

    public static readonly RetentionSweepResult Empty = new(0, 0);
}

public sealed class EventRetention(DbContext db, TimeProvider timeProvider, IObjectStorage storage) : IEventRetention
{
    /// <summary>
    /// Max events mutated per sweep — bounds one transaction. Volumes are tiny;
    /// if a sweep ever fills a page, the next tick simply picks up the rest.
    /// </summary>
    private const int BatchSize = 500;

    public async Task<RetentionSweepResult> SweepAsync(CancellationToken ct = default)
    {
        // Same "today" the guest/host endpoints use (UTC date), so the worker and
        // the on-request auto-close (GuestEventContext) agree on the boundary.
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        // Candidates: live events (Active/UploadClosed) whose upload window or
        // retention window has passed. Draft/Expired/Deleted are never touched.
        //   - Expired branch: any live event past ExpiresAt.
        //   - UploadClosed branch: an Active event past UploadEndsAt (an already
        //     UploadClosed event needs no upload-close flip).
        var due = await db.Set<Event>()
            .Where(e => (e.Status == EventStatus.Active || e.Status == EventStatus.UploadClosed)
                && ((e.ExpiresAt != null && e.ExpiresAt < today)
                    || (e.Status == EventStatus.Active && e.UploadEndsAt != null && e.UploadEndsAt < today)))
            .OrderBy(e => e.ExpiresAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        if (due.Count == 0)
        {
            return RetentionSweepResult.Empty;
        }

        var now = timeProvider.GetUtcNow();
        var uploadClosed = 0;
        var expired = 0;

        foreach (var e in due)
        {
            EventStatus? target = null;
            string? reason = null;

            // Expiry wins over upload-close: UploadEndsAt <= ExpiresAt always, so
            // an event past ExpiresAt goes straight to Expired (no interim flip).
            if (e.ExpiresAt is { } exp && today > exp)
            {
                target = EventStatus.Expired;
                reason = "expiresAt";
            }
            else if (e.Status == EventStatus.Active && e.UploadEndsAt is { } end && today > end)
            {
                target = EventStatus.UploadClosed;
                reason = "uploadEndsAt";
            }

            if (target is not { } newStatus || newStatus == e.Status)
            {
                continue; // nothing to do (idempotent no-op)
            }

            var from = e.Status;
            e.Status = newStatus;

            db.Set<AuditLogEntry>().Add(new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                OccurredAt = now,
                ActorUserId = null, // system / background job, no acting user
                Action = newStatus == EventStatus.Expired ? "event.expired" : "event.upload_closed_auto",
                EntityType = nameof(Event),
                EntityId = e.Id.ToString(),
                Details = JsonSerializer.Serialize(new
                {
                    from = from.ToString(),
                    to = newStatus.ToString(),
                    reason,
                }),
            });

            if (newStatus == EventStatus.Expired)
            {
                expired++;
            }
            else
            {
                uploadClosed++;
            }
        }

        await db.SaveChangesAsync(ct);
        return new RetentionSweepResult(uploadClosed, expired);
    }

    public async Task<IReadOnlyList<Guid>> FindDueForPurgeAsync(int graceDays, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        // today > ExpiresAt + graceDays  ⟺  ExpiresAt < today - graceDays.
        // Comparing plain dates keeps the query trivially translatable (Npgsql
        // does not translate DateOnly.AddDays), and it means "extend retention"
        // (M5) automatically postpones the purge by moving ExpiresAt.
        var cutoff = today.AddDays(-Math.Max(0, graceDays));

        return await db.Set<Event>()
            .Where(e => e.Status == EventStatus.Expired
                && e.ExpiresAt != null
                && e.ExpiresAt < cutoff)
            .OrderBy(e => e.ExpiresAt)
            .Take(BatchSize)
            .Select(e => e.Id)
            .ToListAsync(ct);
    }

    public async Task FinalizeDeletionAsync(
        Guid eventId, int mediaDeleted, int exportsDeleted,
        Guid? actorUserId, EventDeletionCause cause, CancellationToken ct = default)
    {
        var e = await db.Set<Event>().SingleOrDefaultAsync(x => x.Id == eventId, ct);

        // Idempotent: nothing to do if it's gone or already finalized.
        if (e is null || e.Status == EventStatus.Deleted)
        {
            return;
        }

        // The cover photo is the one media object Events owns directly.
        if (e.CoverPhotoKey is { Length: > 0 } coverKey)
        {
            await storage.DeleteAsync(coverKey, ct);
            e.CoverPhotoKey = null;
        }

        e.Status = EventStatus.Deleted;

        var (action, reason) = cause switch
        {
            EventDeletionCause.HostRequest => ("event.deleted_by_host", "host_request"),
            _ => ("event.purged", "retention_grace"),
        };

        db.Set<AuditLogEntry>().Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            OccurredAt = timeProvider.GetUtcNow(),
            ActorUserId = actorUserId, // host for a manual delete, null for the job
            Action = action,
            EntityType = nameof(Event),
            EntityId = e.Id.ToString(),
            Details = JsonSerializer.Serialize(new
            {
                reason,
                mediaDeleted,
                exportsDeleted,
            }),
        });

        await db.SaveChangesAsync(ct);
    }
}
