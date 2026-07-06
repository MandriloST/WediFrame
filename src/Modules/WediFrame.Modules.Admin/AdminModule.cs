using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WediFrame.Shared.Modules;

namespace WediFrame.Modules.Admin;

/// <summary>
/// Internal admin: events, users, storage report, manual interventions.
/// Skeleton only — services and endpoints arrive with their milestone.
/// </summary>
public sealed class AdminModule : IModule
{
    public string Name => "Admin";

    public string Schema => "admin";

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
        => services;

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
        => endpoints;
}
