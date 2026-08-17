using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Opportunity.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddOpportunityModule() => services;
    }
}
