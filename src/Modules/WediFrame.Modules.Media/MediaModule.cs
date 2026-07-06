using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WediFrame.Shared.Modules;

namespace WediFrame.Modules.Media;

/// <summary>
/// Presigned upload flow (single PUT / multipart), metadata, thumbnails, gallery, package limit enforcement.
/// Skeleton only — services and endpoints arrive with their milestone.
/// </summary>
public sealed class MediaModule : IModule
{
    public string Name => "Media";

    public string Schema => "media";

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
        => services;

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
        => endpoints;
}
