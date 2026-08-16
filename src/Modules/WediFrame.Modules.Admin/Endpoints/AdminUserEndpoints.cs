using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WediFrame.Modules.Admin.Contracts;
using WediFrame.Shared.Admin;

namespace WediFrame.Modules.Admin.Endpoints;

/// <summary>
/// Admin-only user list. Reads through the Shared <see cref="IAdminUserDirectory"/>
/// contract (implemented by Identity) — no cross-module reference. Filter by email
/// substring and role; newest first; paginated. The /admin group already enforces
/// the Admin policy.
/// </summary>
public static class AdminUserEndpoints
{
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 200;

    public static IEndpointRouteBuilder MapAdminUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/users", GetUsersAsync);
        return endpoints;
    }

    private static async Task<IResult> GetUsersAsync(
        IAdminUserDirectory users,
        int? page,
        int? pageSize,
        string? q,
        string? role,
        CancellationToken ct)
    {
        var p = page is > 0 ? page.Value : 1;
        var size = pageSize is > 0 ? Math.Min(pageSize.Value, MaxPageSize) : DefaultPageSize;

        var result = await users.ListAsync(new AdminUserQuery(q, role, p, size), ct);

        var items = result.Items
            .Select(u => new AdminUserResponse(u.Id, u.Email, u.Role, u.PreferredLanguage, u.CreatedAt))
            .ToList();

        return Results.Ok(new PagedResponse<AdminUserResponse>(items, p, size, result.Total));
    }
}
