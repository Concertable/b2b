using Concertable.B2B.Dashboard.Artist.Infrastructure;
using Concertable.Shared.Api.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Dashboard.Artist.Api;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddArtistDashboardApi()
        {
            services.AddArtistDashboardModule();
            services.AddControllers()
                .AddInternalControllers(typeof(ArtistDashboardController).Assembly);
            return services;
        }
    }
}
