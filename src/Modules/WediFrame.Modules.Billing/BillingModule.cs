using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WediFrame.Modules.Billing.Endpoints;
using WediFrame.Shared.Modules;

namespace WediFrame.Modules.Billing;

/// <summary>
/// Packages, Free/Trial activation, Stripe checkout + webhook, HR fiscalization + R1 invoice data.
/// M3: Package catalogue (entities + seed + public GET /packages). Checkout/limits arrive next.
/// </summary>
public sealed class BillingModule : IModule
{
    public string Name => "Billing";

    public string Schema => "billing";

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
        => services;

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
        => endpoints.MapPackageEndpoints();
}
