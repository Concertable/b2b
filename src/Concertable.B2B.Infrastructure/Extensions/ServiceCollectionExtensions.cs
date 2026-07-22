using Concertable.B2B.Infrastructure.Uris;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUris(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FrontendUrlSettings>(configuration.GetSection("Urls"));
        services.AddSingleton<IUriGenerator, UriGenerator>();
        services.AddSingleton<IFrontendUriGenerator, FrontendUriGenerator>();

        return services;
    }
}
