using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WediFrame.Modules.Events.Contracts;
using WediFrame.Modules.Events.Services;
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
        IGuestEventAccess guestEvents,
        IObjectStorage storage,
        TimeProvider timeProvider,
        CancellationToken ct)
    {
        // Token validation + visibility rules live in IGuestEventAccess —
        // the single source Media (uploads) uses as well.
        var ev = await guestEvents.FindByTokenAsync(token, ct);
        if (ev is null)
        {
            return Results.NotFound();
        }

        var coverUrl = ev.CoverPhotoKey is null
            ? null
            : (await storage.PresignGetAsync(ev.CoverPhotoKey, EventEndpoints.ViewUrlExpiry, ct)).ToString();

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        return Results.Ok(new GuestEventInfoResponse(
            ev.Title,
            ev.Type,
            ev.UploadStartDate,
            ev.Status.ToString(),
            coverUrl,
            ev.IsUploadOpen(today)));
    }
}
