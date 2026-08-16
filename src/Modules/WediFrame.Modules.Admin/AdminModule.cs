using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WediFrame.Modules.Admin.Endpoints;
using WediFrame.Modules.Admin.Services;
using WediFrame.Shared.Modules;

namespace WediFrame.Modules.Admin;

/// <summary>
/// Internal admin: audit trail (M5 step A1), later events/users/storage report and
/// moderation. Admin is a pure leaf module — it reads the shared audit log directly
/// and reaches other modules only through Shared contracts (e.g. IAdminIdentity),
/// so it references no other module and introduces no cycles.
/// </summary>
public sealed class AdminModule : IModule
{
    public string Name => "Admin";

    public string Schema => "admin";

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AdminOptions>()
            .Bind(configuration.GetSection(AdminOptions.SectionName));

        // Admin-only authorization policy. The JWT carries the role in the "role"
        // claim (RoleClaimType is "role" in the API host), so RequireRole("Admin")
        // matches tokens issued to Admin users. AddAuthorizationBuilder is additive
        // and idempotent alongside the host's AddAuthorization().
        services.AddAuthorizationBuilder()
            .AddPolicy(AdminPolicy.Name, policy => policy.RequireRole("Admin"));

        // Startup promotion of configured admin emails (idempotent, best-effort).
        // Promotes only EXISTING users — no public self-promotion endpoint exists.
        services.AddHostedService<AdminBootstrapService>();

        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/admin").RequireAuthorization(AdminPolicy.Name);
        group.MapAdminAuditEndpoints();
        return endpoints;
    }
}
