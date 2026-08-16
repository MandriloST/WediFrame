using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WediFrame.Modules.Media.Endpoints;
using WediFrame.Modules.Media.Services;
using WediFrame.Shared.Media;
using WediFrame.Shared.Modules;

namespace WediFrame.Modules.Media;

/// <summary>
/// Presigned upload flow (single PUT / multipart), metadata, thumbnails, gallery,
/// package limit enforcement. M1: guest photo upload. M2: gallery + thumbnail worker.
/// </summary>
public sealed class MediaModule : IModule
{
    public string Name => "Media";

    public string Schema => "media";

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ThumbnailOptions>()
            .Bind(configuration.GetSection(ThumbnailOptions.SectionName));

        services.AddOptions<ExportOptions>()
            .Bind(configuration.GetSection(ExportOptions.SectionName));

        // Background thumbnail generation (depends on IThumbnailGenerator +
        // IObjectStorage, both registered by the API host / Infrastructure).
        services.AddHostedService<ThumbnailWorker>();

        // Background gallery ZIP export (depends on IObjectStorage).
        services.AddHostedService<ExportWorker>();

        // Physical erasure of an event's media (R2 + rows). Consumed by the
        // Retention worker after grace (M4, Phase 2) and by host delete (Phase 3).
        services.AddScoped<IEventMediaPurge, EventMediaPurge>();

        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
        => endpoints
            .MapGuestMediaEndpoints()
            .MapHostMediaEndpoints()
            .MapHostStatsEndpoints()
            .MapHostExportEndpoints();
}
