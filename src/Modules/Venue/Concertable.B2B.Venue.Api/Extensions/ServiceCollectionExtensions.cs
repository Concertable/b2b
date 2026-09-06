using Concertable.B2B.Venue.Api.Controllers;
using Concertable.B2B.Venue.Infrastructure.Extensions;
using Concertable.Shared.Api.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Venue.Api.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddVenueApi(IConfiguration configuration)
        {
            services.AddVenueModule(configuration);
            services.AddControllers()
                .AddInternalControllers(typeof(VenueController).Assembly);
            return services;
        }

        public IServiceCollection AddVenueDevSeeder() =>
            Concertable.B2B.Venue.Infrastructure.Extensions.ServiceCollectionExtensions
                .AddVenueDevSeeder(services);
    }
}
