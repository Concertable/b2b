using Concertable.B2B.Dashboard.Artist.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Dashboard.Artist.Infrastructure;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddArtistDashboardModule()
        {
            services.AddScoped<IArtistDashboardService, ArtistDashboardService>();
            return services;
        }
    }
}
