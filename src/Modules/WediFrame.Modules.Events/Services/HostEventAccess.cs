using Microsoft.EntityFrameworkCore;
using WediFrame.Modules.Billing.Services;
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
    EventStatus Status,
    DateOnly UploadStartDate,
    DateOnly? UploadEndsAt,
    DateOnly? ExpiresAt,
    string? PackageSlug,
    string? PackageName,
    GuestUploadLimits? Limits);

public sealed class HostEventAccess(DbContext db, IPackageCatalog packages) : IHostEventAccess
{
    public async Task<HostEventContext?> FindOwnedAsync(
        Guid eventId, Guid ownerUserId, CancellationToken ct = default)
    {
        var row = await db.Set<Event>()
            .Where(e => e.Id == eventId
                && e.OwnerUserId == ownerUserId
                && e.Status != EventStatus.Deleted)
            .Select(e => new
            {
                e.Id,
                e.Title,
                e.Status,
                e.UploadStartDate,
                e.UploadEndsAt,
                e.ExpiresAt,
                e.PackageId,
            })
            .SingleOrDefaultAsync(ct);

        if (row is null)
        {
            return null;
        }

        // Resolve package name + quotas via Billing (no direct Package access).
        string? slug = null;
        string? name = null;
        GuestUploadLimits? limits = null;
        if (row.PackageId is { } packageId
            && await packages.GetByIdAsync(packageId, ct) is { } package)
        {
            slug = package.Slug;
            name = package.Name;
            limits = new GuestUploadLimits(
                package.MaxPhotoCount,
                package.MaxVideoTotalBytes,
                package.MaxTotalBytes,
                package.MaxFileBytes);
        }

        return new HostEventContext(
            row.Id, row.Title, row.Status, row.UploadStartDate, row.UploadEndsAt, row.ExpiresAt, slug, name, limits);
    }
}
