using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WediFrame.Modules.Billing.Endpoints;
using WediFrame.Modules.Billing.Services;
using WediFrame.Shared.Modules;

namespace WediFrame.Modules.Billing;

/// <summary>
/// Packages, Free/Trial activation, Stripe checkout + webhook, HR fiscalization + R1 invoice data.
/// M3: Package catalogue (entities + seed + public GET /packages) and IPackageCatalog for other
/// modules (Events links to a package and derives its timeline). Checkout/limits arrive next.
/// </summary>
public sealed class BillingModule : IModule
{
    public string Name => "Billing";

    public string Schema => "billing";

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        // Cross-module read contract (consumed by Events). Scoped: uses the request DbContext.
        services.AddScoped<IPackageCatalog, PackageCatalog>();
        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
        => endpoints.MapPackageEndpoints();
}
