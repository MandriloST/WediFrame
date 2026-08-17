using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WediFrame.Modules.Admin.Contracts;
using WediFrame.Shared.Admin;

namespace WediFrame.Modules.Admin.Endpoints;

/// <summary>
/// Admin management of partners and bonus codes + the per-partner report, all via the
/// Shared <see cref="IPartnerAdmin"/> contract (implemented by Partners). The /admin
/// group already enforces the Admin policy.
/// </summary>
public static class AdminPartnerEndpoints
{
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 200;

    public static IEndpointRouteBuilder MapAdminPartnerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/partners", ListAsync);
        endpoints.MapPost("/partners", CreateAsync);
        endpoints.MapGet("/partners/{id:guid}", DetailAsync);
        endpoints.MapPost("/partners/{id:guid}/codes", CreateCodeAsync);
        endpoints.MapPatch("/partners/{id:guid}/codes/{codeId:guid}", ToggleCodeAsync);
        endpoints.MapGet("/partners/{id:guid}/report", ReportAsync);
        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        IPartnerAdmin partners, int? page, int? pageSize, string? q, CancellationToken ct)
    {
        var p = page is > 0 ? page.Value : 1;
        var size = pageSize is > 0 ? Math.Min(pageSize.Value, MaxPageSize) : DefaultPageSize;
        var result = await partners.ListPartnersAsync(q, p, size, ct);
        return Results.Ok(new PagedResponse<PartnerRecord>(result.Items, p, size, result.Total));
    }

    private static async Task<IResult> CreateAsync(
        AdminCreatePartnerRequest request, IPartnerAdmin partners, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["name"] = ["partners.name_required"],
            });
        }

        var created = await partners.CreatePartnerAsync(
            new PartnerInput(request.Name, request.Type ?? "Other",
                request.ContactEmail, request.ContactPhone, request.Notes),
            ct);

        return Results.Ok(created);
    }

    private static async Task<IResult> DetailAsync(Guid id, IPartnerAdmin partners, CancellationToken ct)
    {
        var detail = await partners.GetPartnerAsync(id, ct);
        return detail is null ? Results.NotFound() : Results.Ok(detail);
    }

    private static async Task<IResult> CreateCodeAsync(
        Guid id, AdminCreateCodeRequest request, IPartnerAdmin partners, CancellationToken ct)
    {
        var result = await partners.CreateCodeAsync(
            id,
            new BonusCodeInput(
                request.Code ?? "",
                request.DiscountType ?? "Percentage",
                request.DiscountValue,
                request.MaxRedemptions,
                request.ExpiresAt),
            ct);

        return result.Outcome switch
        {
            CreateCodeOutcome.Ok => Results.Ok(result.Code),
            CreateCodeOutcome.PartnerNotFound => Results.NotFound(),
            CreateCodeOutcome.DuplicateCode => Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["code"] = ["partners.code_duplicate"],
            }),
            _ => Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["code"] = ["partners.code_invalid"],
            }),
        };
    }

    private static async Task<IResult> ToggleCodeAsync(
        Guid id, Guid codeId, AdminToggleCodeRequest request, IPartnerAdmin partners, CancellationToken ct)
    {
        var updated = await partners.SetCodeActiveAsync(id, codeId, request.Active, ct);
        return updated is null ? Results.NotFound() : Results.Ok(updated);
    }

    private static async Task<IResult> ReportAsync(Guid id, IPartnerAdmin partners, CancellationToken ct)
    {
        var report = await partners.GetReportAsync(id, ct);
        return report is null ? Results.NotFound() : Results.Ok(report);
    }
}
