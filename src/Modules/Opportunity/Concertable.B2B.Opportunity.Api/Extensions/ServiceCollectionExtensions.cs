using Concertable.B2B.Opportunity.Api.Controllers;
using Concertable.B2B.Opportunity.Api.Mappers;
using Concertable.Shared.Api.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Opportunity.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpportunityApi(this IServiceCollection services)
    {
        services.AddScoped<IOpportunityResponseMapper, OpportunityResponseMapper>();
        services.AddControllers().AddInternalControllers(typeof(OpportunityController).Assembly);
        return services;
    }
}
