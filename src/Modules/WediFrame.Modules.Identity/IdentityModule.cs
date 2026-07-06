using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WediFrame.Shared.Modules;

namespace WediFrame.Modules.Identity;

/// <summary>
/// Host registration/login, JWT, roles (Host, Admin). Guests have NO account.
/// Skeleton only — services and endpoints arrive with their milestone.
/// </summary>
public sealed class IdentityModule : IModule
{
    public string Name => "Identity";

    public string Schema => "identity";

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
        => services;

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
        => endpoints;
}
