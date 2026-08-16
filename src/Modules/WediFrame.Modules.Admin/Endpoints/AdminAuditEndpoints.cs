using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using WediFrame.Modules.Admin.Contracts;
using WediFrame.Shared.Audit;

namespace WediFrame.Modules.Admin.Endpoints;

/// <summary>
/// Admin-only audit trail viewer. Reads <c>shared.audit_log</c> directly — the entity
/// lives in Shared, so no cross-module reference is needed. Filterable by entity type,
/// entity id, action, actor and time window; newest first; paginated. The /admin group
/// already enforces the Admin policy, so no per-route auth here.
/// </summary>
public static class AdminAuditEndpoints
{
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 200;

    public static IEndpointRouteBuilder MapAdminAuditEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/audit", GetAuditAsync);
        return endpoints;
    }

    private static async Task<IResult> GetAuditAsync(
        DbContext db,
        int? page,
        int? pageSize,
        string? entityType,
        string? entityId,
        string? action,
        Guid? actorUserId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken ct)
    {
        var p = page is > 0 ? page.Value : 1;
        var size = pageSize is > 0 ? Math.Min(pageSize.Value, MaxPageSize) : DefaultPageSize;

        var query = db.Set<AuditLogEntry>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(e => e.EntityType == entityType);
        }

        if (!string.IsNullOrWhiteSpace(entityId))
        {
            query = query.Where(e => e.EntityId == entityId);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(e => e.Action == action);
        }

        if (actorUserId is { } actor)
        {
            query = query.Where(e => e.ActorUserId == actor);
        }

        if (from is { } f)
        {
            query = query.Where(e => e.OccurredAt >= f);
        }

        if (to is { } t)
        {
            query = query.Where(e => e.OccurredAt <= t);
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(e => e.OccurredAt)
            .Skip((p - 1) * size)
            .Take(size)
            .Select(e => new AuditLogItemResponse(
                e.Id, e.OccurredAt, e.ActorUserId, e.Action, e.EntityType, e.EntityId, e.Details))
            .ToListAsync(ct);

        return Results.Ok(new PagedResponse<AuditLogItemResponse>(items, p, size, total));
    }
}
