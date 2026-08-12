using Microsoft.EntityFrameworkCore;
using WediFrame.Modules.Events.Domain;

namespace WediFrame.Modules.Events.Services;

/// <summary>
/// The Events module's public contract for HOST-owned access (the mirror of
/// <see cref="IGuestEventAccess"/>). Other modules that need to act on an event
/// on the authenticated host's behalf — Media's gallery management now, later
/// Billing/Admin — consume THIS instead of querying the Event entity directly,
/// so ownership lives in one place and module boundaries stay explicit
/// (ARCHITECTURE.md §2). Registered by <see cref="EventsModule"/>.
/// </summary>
public interface IHostEventAccess
{
    /// <summary>
    /// Resolve an event the given host owns, or null when it does not exist,
    /// is owned by someone else, or is already Deleted. Callers turn null into
    /// a 404 (never 403) so a foreign event id never leaks its existence —
    /// the same rule the Events endpoints use internally.
    /// </summary>
    Task<HostEventContext?> FindOwnedAsync(Guid eventId, Guid ownerUserId, CancellationToken ct = default);
}

/// <summary>Read-only slice of an event that host-facing features in other modules need.</summary>
public sealed record HostEventContext(
    Guid EventId,
    string Title,
    EventStatus Status);

public sealed class HostEventAccess(DbContext db) : IHostEventAccess
{
    public async Task<HostEventContext?> FindOwnedAsync(
        Guid eventId, Guid ownerUserId, CancellationToken ct = default)
        => await db.Set<Event>()
            .Where(e => e.Id == eventId
                && e.OwnerUserId == ownerUserId
                && e.Status != EventStatus.Deleted)
            .Select(e => new HostEventContext(e.Id, e.Title, e.Status))
            .SingleOrDefaultAsync(ct);
}
