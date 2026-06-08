using Concertable.B2B.Venue.Api.Controllers;
using Concertable.B2B.Venue.Infrastructure.Extensions;
using Concertable.Shared.Api.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Venue.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddVenueApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddVenueModule(configuration);
        services.AddControllers()
            .AddInternalControllers(typeof(VenueController).Assembly);
        return services;
    }
}
