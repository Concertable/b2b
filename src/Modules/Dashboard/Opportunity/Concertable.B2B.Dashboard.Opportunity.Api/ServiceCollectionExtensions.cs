using Concertable.B2B.Dashboard.Opportunity.Infrastructure;
using Concertable.Shared.Api.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Dashboard.Opportunity.Api;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddOpportunityDashboardApi()
        {
            services.AddOpportunityDashboardModule();
            services.AddControllers()
                .AddInternalControllers(typeof(OpportunityDashboardController).Assembly);
            return services;
        }
    }
}
