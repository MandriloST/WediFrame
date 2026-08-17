using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WediFrame.Modules.Admin.Contracts;
using WediFrame.Shared.Admin;
using WediFrame.Shared.Auth;

namespace WediFrame.Modules.Admin.Endpoints;

/// <summary>
/// Admin-only moderation of any event's media: list, hide/unhide, soft-delete.
/// Goes through the Shared <see cref="IAdminMedia"/> contract (implemented by Media);
/// no ownership check (admin is trusted), actions audited with the admin as actor.
/// The /admin group already enforces the Admin policy.
/// </summary>
public static class AdminMediaEndpoints
{
    private const int DefaultPageSize = 24;
    private const int MaxPageSize = 48;

    public static IEndpointRouteBuilder MapAdminMediaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/events/{eventId:guid}/media", ListAsync);
        endpoints.MapPatch("/events/{eventId:guid}/media/{mediaId:guid}", SetVisibilityAsync);
        endpoints.MapDelete("/events/{eventId:guid}/media/{mediaId:guid}", DeleteAsync);
        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        Guid eventId,
        IAdminMedia media,
        int? offset,
        int? limit,
        CancellationToken ct)
    {
        var skip = Math.Max(0, offset ?? 0);
        var take = Math.Clamp(limit ?? DefaultPageSize, 1, MaxPageSize);
        var page = await media.ListAsync(eventId, skip, take, ct);
        return Results.Ok(page);
    }

    private static async Task<IResult> SetVisibilityAsync(
        Guid eventId,
        Guid mediaId,
        AdminSetVisibilityRequest request,
        ClaimsPrincipal principal,
        IAdminMedia media,
        CancellationToken ct)
    {
        if (principal.GetUserId() is not { } adminUserId)
        {
            return Results.Unauthorized();
        }

        var value = (request.Visibility ?? "").Trim();
        bool hidden;
        if (string.Equals(value, "Hidden", StringComparison.OrdinalIgnoreCase))
        {
            hidden = true;
        }
        else if (string.Equals(value, "Visible", StringComparison.OrdinalIgnoreCase))
        {
            hidden = false;
        }
        else
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["visibility"] = ["media.visibility_invalid"], // Visible | Hidden
            });
        }

        var result = await media.SetVisibilityAsync(eventId, mediaId, hidden, adminUserId, ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> DeleteAsync(
        Guid eventId,
        Guid mediaId,
        ClaimsPrincipal principal,
        IAdminMedia media,
        CancellationToken ct)
    {
        if (principal.GetUserId() is not { } adminUserId)
        {
            return Results.Unauthorized();
        }

        var found = await media.SoftDeleteAsync(eventId, mediaId, adminUserId, ct);
        return found ? Results.NoContent() : Results.NotFound();
    }
}
