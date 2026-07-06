using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WediFrame.Shared.Modules;

namespace WediFrame.Modules.Partners;

/// <summary>
/// Partners, bonus codes, redemption attribution, per-partner report.
/// Skeleton only — services and endpoints arrive with their milestone.
/// </summary>
public sealed class PartnersModule : IModule
{
    public string Name => "Partners";

    public string Schema => "partners";

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
        => services;

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
        => endpoints;
}
