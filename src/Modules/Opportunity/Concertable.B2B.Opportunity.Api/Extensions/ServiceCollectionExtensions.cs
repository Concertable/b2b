using Concertable.B2B.Opportunity.Api.Controllers;
using Concertable.B2B.Opportunity.Api.Mappers;
using Concertable.B2B.Opportunity.Infrastructure.Extensions;
using Concertable.Shared.Api.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Opportunity.Api.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddOpportunityApi(IConfiguration configuration)
        {
            services.AddOpportunityModule(configuration);
            services.AddOpportunityDevSeeder();
            services.AddScoped<IOpportunityResponseMapper, OpportunityResponseMapper>();
            services.AddControllers().AddInternalControllers(typeof(OpportunityController).Assembly);
            return services;
        }
    }
}
