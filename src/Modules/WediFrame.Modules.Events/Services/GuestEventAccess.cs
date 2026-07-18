using Microsoft.EntityFrameworkCore;
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

/// <summary>Read-only slice of an event that guest-facing features need.</summary>
public sealed record GuestEventContext(
    Guid EventId,
    string Title,
    string Type,
    EventStatus Status,
    DateOnly UploadStartDate,
    string? CoverPhotoKey)
{
    /// <summary>
    /// Provisional until packages exist (M3): the upload period end is unknown,
    /// so "open" = Active status + today has reached T0. The Retention module
    /// will flip status to UploadClosed once packages define the period.
    /// </summary>
    public bool IsUploadOpen(DateOnly today) => Status == EventStatus.Active && today >= UploadStartDate;
}

public sealed class GuestEventAccess(DbContext db) : IGuestEventAccess
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
        return await db.Set<Event>()
            .Where(e => e.GuestToken == token
                && (e.Status == EventStatus.Active || e.Status == EventStatus.UploadClosed))
            .Select(e => new GuestEventContext(e.Id, e.Title, e.Type, e.Status, e.UploadStartDate, e.CoverPhotoKey))
            .SingleOrDefaultAsync(ct);
    }
}
