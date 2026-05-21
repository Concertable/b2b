using Concertable.Artist.Api.Controllers;
using Concertable.Artist.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Artist.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddArtistApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddArtistModule(configuration);
        services.AddControllers()
            .AddApplicationPart(typeof(ArtistController).Assembly)
            .ConfigureApplicationPartManager(apm =>
                apm.FeatureProviders.Add(new InternalControllerFeatureProvider()));
        return services;
    }
}
