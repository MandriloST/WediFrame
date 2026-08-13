using Microsoft.EntityFrameworkCore;
using WediFrame.Modules.Billing.Services;
using WediFrame.Modules.Events.Domain;

namespace WediFrame.Modules.Events.Services;

/// <summary>
/// The Events module's public contract for guest-token access. Other modules
/// (Media now, later Retention) consume THIS instead of querying the Event
/// entity directly — keeps module boundaries explicit (ARCHITECTURE.md §2).
/// Registered by <see cref="EventsModule"/>.
/// </summary>
public interface IGuestEventAccess
{
    /// <summary>
    /// Resolve a guest token to an event context, or null when the token is
    /// unknown or the event must stay invisible to guests (Draft/Expired/Deleted).
    /// </summary>
    Task<GuestEventContext?> FindByTokenAsync(string token, CancellationToken ct = default);
}

/// <summary>
/// The three states the guest page distinguishes for the upload button.
/// (The guest never sees Draft/Expired/Deleted — those events aren't guest-visible.)
/// </summary>
public enum GuestUploadState
{
    /// <summary>Event is Active but today hasn't reached T0 yet — uploads open later.</summary>
    NotStarted,

    /// <summary>Uploads are accepted right now.</summary>
    Open,

    /// <summary>Upload period is over — the gallery stays, but no new uploads.</summary>
    Closed,
}

/// <summary>
/// Per-event upload quotas, resolved from the event's package (Billing, M3).
/// Null on a legacy event without a package → Media falls back to per-file caps only.
/// </summary>
public sealed record GuestUploadLimits(
    int MaxPhotoCount,
    long MaxVideoTotalBytes,
    long MaxTotalBytes,
    long MaxFileBytes);

/// <summary>Read-only slice of an event that guest-facing features need.</summary>
public sealed record GuestEventContext(
    Guid EventId,
    string Title,
    string Type,
    EventStatus Status,
    DateOnly UploadStartDate,
    DateOnly? UploadEndsAt,
    GuestUploadLimits? Limits,
    string? CoverPhotoKey)
{
    /// <summary>
    /// Which upload state to show the guest. Uploads auto-close once the package
    /// period passes (<see cref="UploadEndsAt"/>) — no Retention job needed for the
    /// guest-facing behavior; Retention (M4) additionally persists the status flip.
    /// A host can also close early (status UploadClosed). START is T0.
    /// </summary>
    public GuestUploadState UploadStateFor(DateOnly today) =>
        Status == EventStatus.UploadClosed || (UploadEndsAt is { } end && today > end)
            ? GuestUploadState.Closed
            : today < UploadStartDate
                ? GuestUploadState.NotStarted
                : GuestUploadState.Open;

    /// <summary>
    /// Whether an upload is allowed right now. Kept as the single guard Media
    /// uses — identical to <see cref="UploadStateFor"/> returning Open.
    /// </summary>
    public bool IsUploadOpen(DateOnly today) => UploadStateFor(today) == GuestUploadState.Open;
}

public sealed class GuestEventAccess(DbContext db, IPackageCatalog packages) : IGuestEventAccess
{
    public async Task<GuestEventContext?> FindByTokenAsync(string token, CancellationToken ct = default)
    {
        // Guest tokens are 43-char Base64Url strings; anything wildly off is a
        // cheap early reject before touching the database.
        if (string.IsNullOrWhiteSpace(token) || token.Length is < 20 or > 100)
        {
            return null;
        }

        // Visibility rule (Decision Log v7): guests see Active/UploadClosed only.
        var row = await db.Set<Event>()
            .Where(e => e.GuestToken == token
                && (e.Status == EventStatus.Active || e.Status == EventStatus.UploadClosed))
            .Select(e => new
            {
                e.Id,
                e.Title,
                e.Type,
                e.Status,
                e.UploadStartDate,
                e.UploadEndsAt,
                e.PackageId,
                e.CoverPhotoKey,
            })
            .SingleOrDefaultAsync(ct);

        if (row is null)
        {
            return null;
        }

        // Resolve package quotas via Billing (no direct Package access — module boundary).
        GuestUploadLimits? limits = null;
        if (row.PackageId is { } packageId
            && await packages.GetByIdAsync(packageId, ct) is { } package)
        {
            limits = new GuestUploadLimits(
                package.MaxPhotoCount,
                package.MaxVideoTotalBytes,
                package.MaxTotalBytes,
                package.MaxFileBytes);
        }

        return new GuestEventContext(
            row.Id, row.Title, row.Type, row.Status, row.UploadStartDate, row.UploadEndsAt, limits, row.CoverPhotoKey);
    }
}
