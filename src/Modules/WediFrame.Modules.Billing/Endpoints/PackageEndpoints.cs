using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using WediFrame.Modules.Billing.Contracts;
using WediFrame.Modules.Billing.Domain;

namespace WediFrame.Modules.Billing.Endpoints;

/// <summary>
/// Public packages endpoint (ARCHITECTURE.md §4): the pricing page reads the 5
/// active packages. No auth — this is public catalogue data. Checkout, bonus
/// codes and R1/fiscalization arrive later in M3.
/// </summary>
public static class PackageEndpoints
{
    public static IEndpointRouteBuilder MapPackageEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Public on purpose — no RequireAuthorization.
        endpoints.MapGet("/packages", GetPackagesAsync);

        return endpoints;
    }

    private static async Task<IResult> GetPackagesAsync(DbContext db, CancellationToken ct)
    {
        var packages = await db.Set<Package>()
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .Select(p => new PackageResponse(
                p.Slug,
                p.Name,
                p.PriceCents,
                p.Currency,
                p.MaxPhotoCount,
                p.MaxVideoTotalBytes,
                p.MaxTotalBytes,
                p.MaxFileBytes,
                p.UploadPeriodDays,
                p.RetentionDays,
                p.SortOrder))
            .ToListAsync(ct);

        return Results.Ok(packages);
    }
}
