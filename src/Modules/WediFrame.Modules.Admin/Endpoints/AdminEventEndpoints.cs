using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WediFrame.Modules.Admin.Contracts;
using WediFrame.Shared.Admin;
using WediFrame.Shared.Directory;

namespace WediFrame.Modules.Admin.Endpoints;

/// <summary>
/// Admin-only event browser. Reads events through the Shared <see cref="IAdminEventDirectory"/>
/// (implemented by Events) and resolves owner emails through <see cref="IUserDirectory"/>
/// (implemented by Identity) — Admin references neither module, only the Shared ports.
/// Filter by title substring and status; newest first; paginated. The /admin group
/// already enforces the Admin policy.
/// </summary>
public static class AdminEventEndpoints
{
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 200;

    public static IEndpointRouteBuilder MapAdminEventEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/events", GetEventsAsync);
        endpoints.MapGet("/events/{id:guid}", GetEventAsync);
        return endpoints;
    }

    private static async Task<IResult> GetEventsAsync(
        IAdminEventDirectory events,
        IUserDirectory users,
        int? page,
        int? pageSize,
        string? q,
        string? status,
        CancellationToken ct)
    {
        var p = page is > 0 ? page.Value : 1;
        var size = pageSize is > 0 ? Math.Min(pageSize.Value, MaxPageSize) : DefaultPageSize;

        var result = await events.ListAsync(new AdminEventQuery(q, status, p, size), ct);

        var ownerIds = result.Items.Select(e => e.OwnerUserId).Distinct().ToArray();
        var contacts = ownerIds.Length > 0
            ? await users.GetContactsAsync(ownerIds, ct)
            : new Dictionary<Guid, UserContact>();

        var items = result.Items
            .Select(e => new AdminEventResponse(
                e.Id, e.Title, e.Type, e.Status,
                e.OwnerUserId,
                contacts.TryGetValue(e.OwnerUserId, out var c) ? c.Email : null,
                e.PackageSlug, e.PackageName,
                e.UploadStartDate, e.UploadEndsAt, e.ExpiresAt,
                e.HasCover, e.CreatedAt))
            .ToList();

        return Results.Ok(new PagedResponse<AdminEventResponse>(items, p, size, result.Total));
    }

    private static async Task<IResult> GetEventAsync(
        Guid id,
        IAdminEventDirectory events,
        IUserDirectory users,
        CancellationToken ct)
    {
        var e = await events.GetAsync(id, ct);
        if (e is null)
        {
            return Results.NotFound();
        }

        var contact = await users.GetContactAsync(e.OwnerUserId, ct);

        return Results.Ok(new AdminEventDetailResponse(
            e.Id, e.Title, e.Type, e.Status,
            e.OwnerUserId, contact?.Email,
            e.PackageSlug, e.PackageName,
            e.UploadStartDate, e.UploadEndsAt, e.ExpiresAt,
            e.HasCover, e.GuestToken, e.GuestUrl, e.CreatedAt));
    }
}
