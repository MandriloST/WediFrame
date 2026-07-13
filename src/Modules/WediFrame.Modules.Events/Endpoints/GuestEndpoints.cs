using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using WediFrame.Modules.Events.Contracts;
using WediFrame.Modules.Events.Domain;
using WediFrame.Shared.Storage;

namespace WediFrame.Modules.Events.Endpoints;

/// <summary>
/// Guest-facing endpoints — NO authentication, the long unguessable event token
/// is the only key (guests have no accounts by design). M1 scope: event info
/// for the guest page hero (title + cover). Uploads and gallery arrive with Media.
/// Rate limiting on these routes is an M5 backlog item.
/// </summary>
public static class GuestEndpoints
{
    public static IEndpointRouteBuilder MapGuestEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Deliberately outside RequireAuthorization — token IS the authorization.
        endpoints.MapGet("/guest/{token}", GetEventInfoAsync);

        return endpoints;
    }

    private static async Task<IResult> GetEventInfoAsync(
        string token,
        DbContext db,
        IObjectStorage storage,
        TimeProvider timeProvider,
        CancellationToken ct)
    {
        // Guest tokens are 43-char Base64Url strings; anything wildly off is a
        // cheap early reject before touching the database.
        if (string.IsNullOrWhiteSpace(token) || token.Length is < 20 or > 100)
        {
            return Results.NotFound();
        }

        // Visibility rules (assumption, flagged in Decision Log):
        //   Draft/Deleted/Expired -> 404 (guests must never see them),
        //   Active/UploadClosed   -> visible (gallery stays after upload closes).
        var entity = await db.Set<Event>()
            .SingleOrDefaultAsync(e => e.GuestToken == token
                && (e.Status == EventStatus.Active || e.Status == EventStatus.UploadClosed), ct);

        if (entity is null)
        {
            return Results.NotFound();
        }

        var coverUrl = entity.CoverPhotoKey is null
            ? null
            : (await storage.PresignGetAsync(entity.CoverPhotoKey, EventEndpoints.ViewUrlExpiry, ct)).ToString();

        // Provisional until packages exist (M3): upload period end is unknown,
        // so "open" = Active status + today has reached T0.
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var uploadOpen = entity.Status == EventStatus.Active && today >= entity.UploadStartDate;

        return Results.Ok(new GuestEventInfoResponse(
            entity.Title,
            entity.Type,
            entity.UploadStartDate,
            entity.Status.ToString(),
            coverUrl,
            uploadOpen));
    }
}
