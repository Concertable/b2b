using Concertable.B2B.Dashboard.Venue.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Dashboard.Venue.Infrastructure;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddVenueDashboardModule()
        {
            services.AddScoped<IVenueDashboardService, VenueDashboardService>();
            return services;
        }
    }
}
