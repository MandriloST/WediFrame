using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WediFrame.Modules.Partners.Services;
using WediFrame.Shared.Modules;

namespace WediFrame.Modules.Partners;

/// <summary>
/// Partners, bonus codes, redemption attribution, per-partner report.
/// P1 (v45): entities + admin management/report via the Shared IPartnerAdmin port.
/// Checkout attribution + discount (P2) arrive later. Admin-facing endpoints live in
/// the Admin module (under /admin/partners), so this module exposes no HTTP surface yet.
/// </summary>
public sealed class PartnersModule : IModule
{
    public string Name => "Partners";

    public string Schema => "partners";

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<WediFrame.Shared.Admin.IPartnerAdmin, PartnerAdmin>();
        services.AddScoped<WediFrame.Shared.Partners.IBonusCodeService, BonusCodeService>();
        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
        => endpoints;
}
