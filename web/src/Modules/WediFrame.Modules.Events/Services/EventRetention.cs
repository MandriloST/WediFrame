using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WediFrame.Modules.Events.Domain;
using WediFrame.Shared.Audit;

namespace WediFrame.Modules.Events.Services;

/// <summary>
/// The Events module's public contract for retention-driven status transitions.
/// The Retention module (M4) owns the *schedule* (a background worker), but the
/// mutation of an <see cref="Event"/> lives here — in the module that owns the
/// entity — so status rules stay in one place and Retention only references
/// Events (one-way, no cycle; ARCHITECTURE.md §2).
///
/// Phase 1 (this): time-based status flips only, NO physical deletion.
///   Active                 → UploadClosed   when today &gt; UploadEndsAt
///   Active / UploadClosed  → Expired        when today &gt; ExpiresAt
/// Guest access already treats anything other than Active/UploadClosed as
/// unavailable, so Expired means the gallery is no longer reachable. Grace
/// period + hard-delete of R2 media arrive in Phase 2.
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
}

/// <summary>Counts from one retention sweep.</summary>
public sealed record RetentionSweepResult(int UploadClosed, int Expired)
{
    public int Total => UploadClosed + Expired;

    public static readonly RetentionSweepResult Empty = new(0, 0);
}

public sealed class EventRetention(DbContext db, TimeProvider timeProvider) : IEventRetention
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
}
