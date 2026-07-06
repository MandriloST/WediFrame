using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WediFrame.Shared.Modules;

namespace WediFrame.Modules.Retention;

/// <summary>
/// Background jobs: close upload period -> retention reminder -> soft delete -> grace -> hard delete.
/// Skeleton only — services and endpoints arrive with their milestone.
/// </summary>
public sealed class RetentionModule : IModule
{
    public string Name => "Retention";

    public string Schema => "retention";

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
        => services;

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
        => endpoints;
}
