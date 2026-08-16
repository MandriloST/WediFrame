using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WediFrame.Modules.Billing.Configuration;
using WediFrame.Modules.Billing.Endpoints;
using WediFrame.Modules.Billing.Services;
using WediFrame.Shared.Modules;

namespace WediFrame.Modules.Billing;

/// <summary>
/// Packages, Free/Trial activation, Stripe checkout + webhook, HR fiscalization + R1 invoice data.
/// M3: Package catalogue + IPackageCatalog; Purchase entity; fiscalization behind a swappable
/// port (manual default, Parra adapter). Stripe checkout/webhook arrive next.
/// </summary>
public sealed class BillingModule : IModule
{
    public string Name => "Billing";

    public string Schema => "billing";

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        // Cross-module read contract (consumed by Events). Scoped: uses the request DbContext.
        services.AddScoped<IPackageCatalog, PackageCatalog>();

        // Fiscalization: pick the provider by config, behind a single port so it's swappable.
        var section = configuration.GetSection(FiscalizationOptions.SectionName);
        services.AddOptions<FiscalizationOptions>().Bind(section);

        var provider = section.GetValue<string>("Provider") ?? "manual";
        if (string.Equals(provider, "parra", StringComparison.OrdinalIgnoreCase))
        {
            var baseUrl = section.GetSection("Parra").GetValue<string>("BaseUrl") ?? "https://api.parra.hr";
            var apiKey = section.GetSection("Parra").GetValue<string>("ApiKey") ?? "";
            services.AddHttpClient<IFiscalizationService, ParraFiscalizationService>(client =>
            {
                client.BaseAddress = new Uri(baseUrl);
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    // Server-side only; exact auth header confirmed with Parra when the adapter is finalized.
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                }
            });
        }
        else
        {
            services.AddScoped<IFiscalizationService, ManualFiscalizationService>();
        }

        // Payment gateway (Stripe) + checkout orchestration. Stripe is behind
        // IPaymentGateway so it's swappable too.
        services.AddOptions<StripeOptions>().Bind(configuration.GetSection(StripeOptions.SectionName));
        services.AddScoped<IPaymentGateway, StripePaymentGateway>();
        services.AddScoped<ICheckoutService, CheckoutService>();

        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
        => endpoints.MapPackageEndpoints();
}
