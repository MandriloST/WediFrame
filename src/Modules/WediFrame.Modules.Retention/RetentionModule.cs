using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WediFrame.Modules.Retention.Services;
using WediFrame.Shared.Modules;

namespace WediFrame.Modules.Retention;

/// <summary>
/// Background jobs for the event lifecycle end. M4 Phase 1 (this): automatic
/// status transitions (Active → UploadClosed → Expired) via <see cref="RetentionWorker"/>.
/// The worker only schedules; the Event mutation lives in the Events module behind
/// IEventRetention (Retention → Events, one-way). Later phases: retention reminder,
/// grace period + hard delete of R2 media.
/// </summary>
public sealed class RetentionModule : IModule
{
    public string Name => "Retention";

    public string Schema => "retention";

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RetentionOptions>()
            .Bind(configuration.GetSection(RetentionOptions.SectionName));

        // Background status transitions (depends on IEventRetention from Events).
        services.AddHostedService<RetentionWorker>();

        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
        => endpoints;
}
