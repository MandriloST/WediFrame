using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WediFrame.Modules.Admin.Contracts;
using WediFrame.Shared.Admin;

namespace WediFrame.Modules.Admin.Endpoints;

/// <summary>
/// Admin dashboard: system-wide aggregates pulled entirely through Shared ports —
/// user count (Identity), events-by-status (Events), storage totals + top events
/// (Media). Admin references no module, only the contracts.
/// </summary>
public static class AdminOverviewEndpoints
{
    private const int TopEventsLimit = 10;

    public static IEndpointRouteBuilder MapAdminOverviewEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/overview", GetOverviewAsync);
        return endpoints;
    }

    private static async Task<IResult> GetOverviewAsync(
        IAdminUserDirectory users,
        IAdminEventDirectory events,
        IAdminStorage storage,
        CancellationToken ct)
    {
        // User count: reuse the paged directory (total is all we need here).
        var usersPage = await users.ListAsync(new AdminUserQuery(null, null, 1, 1), ct);

        var statusCounts = await events.GetStatusCountsAsync(ct);
        var eventsTotal = statusCounts.Values.Sum();

        var totals = await storage.GetTotalsAsync(ct);

        var top = await storage.TopEventsByStorageAsync(TopEventsLimit, ct);
        var titles = await events.GetTitlesAsync(top.Select(t => t.EventId).ToArray(), ct);

        var topEvents = top
            .Select(t => new AdminStorageEventResponse(
                t.EventId,
                titles.TryGetValue(t.EventId, out var title) ? title : null,
                t.Bytes,
                t.ItemCount))
            .ToList();

        return Results.Ok(new AdminOverviewResponse(
            new AdminOverviewUsers(usersPage.Total),
            new AdminOverviewEvents(eventsTotal, statusCounts),
            new AdminOverviewStorage(
                totals.TotalBytes, totals.ItemCount, totals.PhotoCount, totals.VideoCount, topEvents)));
    }
}
