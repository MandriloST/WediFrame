using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WediFrame.Shared.Modules;

namespace WediFrame.Modules.Events;

/// <summary>
/// Event lifecycle (draft -> active -> upload-closed -> expired -> deleted), guest token, QR, settings.
/// Skeleton only — services and endpoints arrive with their milestone.
/// </summary>
public sealed class EventsModule : IModule
{
    public string Name => "Events";

    public string Schema => "events";

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
        => services;

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
        => endpoints;
}
