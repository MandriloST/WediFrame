using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WediFrame.Shared.Modules;

/// <summary>
/// Contract every WediFrame module implements. The API host discovers modules
/// explicitly (no reflection magic) and calls these in order:
/// RegisterServices at startup, MapEndpoints after the app is built.
/// </summary>
public interface IModule
{
    /// <summary>Human-readable module name (used in logs/diagnostics).</summary>
    string Name { get; }

    /// <summary>PostgreSQL schema owned by this module.</summary>
    string Schema { get; }

    /// <summary>Register the module's services into the shared DI container.</summary>
    IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration);

    /// <summary>Map the module's endpoints. Receives the /api/v1 route group.</summary>
    IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints);
}
