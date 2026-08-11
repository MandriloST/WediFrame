using Microsoft.Extensions.DependencyInjection;
using WediFrame.Shared.Imaging;

namespace WediFrame.Infrastructure.Imaging;

public static class ImagingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the libvips-backed <see cref="IThumbnailGenerator"/>. Kept in
    /// Infrastructure (a technology concern, like R2) so the Media module's
    /// worker depends only on the abstraction.
    /// </summary>
    public static IServiceCollection AddImaging(this IServiceCollection services)
    {
        services.AddSingleton<IThumbnailGenerator, NetVipsThumbnailGenerator>();
        return services;
    }
}
