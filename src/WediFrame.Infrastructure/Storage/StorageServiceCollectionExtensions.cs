using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WediFrame.Shared.Options;
using WediFrame.Shared.Storage;

namespace WediFrame.Infrastructure.Storage;

public static class StorageServiceCollectionExtensions
{
    /// <summary>
    /// Registers Cloudflare R2 as the <see cref="IObjectStorage"/> implementation.
    /// Deliberately NOT validated on start: the API must boot without R2 config
    /// (health, auth, event CRUD all work); the first storage call throws a
    /// clear error instead. Options come from the "R2" configuration section.
    /// </summary>
    public static IServiceCollection AddR2Storage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<R2Options>()
            .Bind(configuration.GetSection(R2Options.SectionName));

        services.AddSingleton<IObjectStorage, R2ObjectStorage>();

        return services;
    }
}
