using Concertable.Concert.Api.Controllers;
using Concertable.Concert.Api.Handlers;
using Concertable.Concert.Api.Mappers;
using Concertable.Concert.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Concert.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConcertApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddConcertModule(configuration);
        services.AddScoped<IConcertPostedHandler, ConcertPostedHandler>();
        services.AddSingleton<IApplicationResponseMapper, ApplicationResponseMapper>();
        services.AddSingleton<IOpportunityResponseMapper, OpportunityResponseMapper>();
        services.AddControllers()
            .AddApplicationPart(typeof(ConcertController).Assembly)
            .ConfigureApplicationPartManager(apm =>
                apm.FeatureProviders.Add(new InternalControllerFeatureProvider()));
        return services;
    }
}
