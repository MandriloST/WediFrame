using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WediFrame.Modules.Events.Endpoints;
using WediFrame.Modules.Events.Services;
using WediFrame.Shared.Modules;
using WediFrame.Shared.Options;

namespace WediFrame.Modules.Events;

/// <summary>
/// Event lifecycle (draft -> active -> upload-closed -> expired -> deleted),
/// guest token, QR, settings. M1 scope: create draft + list/detail + QR.
/// </summary>
public sealed class EventsModule : IModule
{
    public string Name => "Events";

    public string Schema => "events";

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<FrontendOptions>()
            .Bind(configuration.GetSection(FrontendOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.GuestBaseUrl),
                "Frontend:GuestBaseUrl must be set (e.g. http://localhost:3000/e/ locally, Frontend__GuestBaseUrl env var on Railway).")
            .ValidateOnStart();

        services.AddSingleton<IQrCodeService, QrCodeService>();

        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
        => endpoints.MapEventEndpoints().MapGuestEndpoints();
}
