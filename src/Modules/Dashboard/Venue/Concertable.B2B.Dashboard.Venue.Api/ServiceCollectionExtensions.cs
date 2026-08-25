using Concertable.B2B.Dashboard.Venue.Infrastructure;
using Concertable.Shared.Api.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Dashboard.Venue.Api;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddVenueDashboardApi()
        {
            services.AddVenueDashboardModule();
            services.AddControllers()
                .AddInternalControllers(typeof(VenueDashboardController).Assembly);
            return services;
        }
    }
}
