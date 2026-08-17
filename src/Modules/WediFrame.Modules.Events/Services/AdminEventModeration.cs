using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WediFrame.Modules.Events.Domain;
using WediFrame.Shared.Admin;
using WediFrame.Shared.Audit;

namespace WediFrame.Modules.Events.Services;

/// <summary>
/// Events-side implementation of <see cref="IAdminEventModeration"/>. Keeps the
/// Event mutation (and its status rules) inside the module that owns the entity;
/// the grace-purge derives from ExpiresAt, so pushing ExpiresAt out automatically
/// postpones physical deletion (see EventRetention.FindDueForPurgeAsync).
/// </summary>
public sealed class AdminEventModeration(DbContext db, TimeProvider timeProvider) : IAdminEventModeration
{
    public async Task<AdminRetentionResult> ExtendRetentionAsync(
        Guid eventId, DateOnly newExpiresAt, Guid adminUserId, CancellationToken ct)
    {
        var e = await db.Set<Event>().SingleOrDefaultAsync(x => x.Id == eventId, ct);
        if (e is null)
        {
            return new AdminRetentionResult(AdminRetentionOutcome.NotFound);
        }

        // Must be an activated, non-deleted event (has an ExpiresAt to move).
        if (e.Status == EventStatus.Deleted || e.ExpiresAt is not { } currentExpires)
        {
            return new AdminRetentionResult(AdminRetentionOutcome.NotActivated);
        }

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        // Extension only: the new date must be in the future AND later than now.
        if (newExpiresAt <= currentExpires || newExpiresAt <= today)
        {
            return new AdminRetentionResult(AdminRetentionOutcome.NotLater);
        }

        var fromStatus = e.Status;
        e.ExpiresAt = newExpiresAt;

        // Revive an expired gallery so the extension actually restores access.
        // Uploads stay closed — extending retention is not reopening uploads.
        if (e.Status == EventStatus.Expired)
        {
            e.Status = EventStatus.UploadClosed;
        }

        db.Set<AuditLogEntry>().Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            OccurredAt = timeProvider.GetUtcNow(),
            ActorUserId = adminUserId,
            Action = "event.retention_extended",
            EntityType = nameof(Event),
            EntityId = e.Id.ToString(),
            Details = JsonSerializer.Serialize(new
            {
                fromExpiresAt = currentExpires.ToString("yyyy-MM-dd"),
                toExpiresAt = newExpiresAt.ToString("yyyy-MM-dd"),
                fromStatus = fromStatus.ToString(),
                toStatus = e.Status.ToString(),
            }),
        });

        await db.SaveChangesAsync(ct);

        return new AdminRetentionResult(AdminRetentionOutcome.Ok, e.ExpiresAt, e.Status.ToString());
    }
}
