using Concertable.B2B.Artist.Api.Controllers;
using Concertable.B2B.Artist.Infrastructure.Extensions;
using Concertable.Shared.Api.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Artist.Api.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddArtistApi(IConfiguration configuration)
        {
            services.AddArtistModule(configuration);
            services.AddControllers()
                .AddInternalControllers(typeof(ArtistController).Assembly);
            return services;
        }

        public IServiceCollection AddArtistDevSeeder() =>
            Concertable.B2B.Artist.Infrastructure.Extensions.ServiceCollectionExtensions
                .AddArtistDevSeeder(services);
    }
}
