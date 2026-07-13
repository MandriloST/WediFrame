using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WediFrame.Modules.Identity.Domain;
using WediFrame.Modules.Identity.Endpoints;
using WediFrame.Modules.Identity.Services;
using WediFrame.Shared.Modules;

namespace WediFrame.Modules.Identity;

/// <summary>
/// Host registration/login, JWT + rotating refresh tokens, roles (Host, Admin).
/// Guests have NO account — they are authorized by the event token (Events module).
/// </summary>
public sealed class IdentityModule : IModule
{
    public string Name => "Identity";

    public string Schema => "identity";

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.SigningKey) && o.SigningKey.Length >= 32,
                "Jwt:SigningKey must be set and at least 32 characters (user-secrets locally, Jwt__SigningKey env var on Railway).")
            .ValidateOnStart();

        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<ITokenService, TokenService>();

        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
        => endpoints.MapAuthEndpoints();
}
