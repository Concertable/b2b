using Concertable.B2B.Artist.Api.Controllers;
using Concertable.B2B.Artist.Infrastructure.Extensions;
using Concertable.Shared.Api.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Artist.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddArtistApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddArtistModule(configuration);
        services.AddControllers()
            .AddInternalControllers(typeof(ArtistController).Assembly);
        return services;
    }
}
