using Concertable.B2B.Dashboard.Opportunity.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Dashboard.Opportunity.Infrastructure;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddOpportunityDashboardModule()
        {
            services.AddScoped<IOpportunityDashboardService, OpportunityDashboardService>();
            return services;
        }
    }
}
