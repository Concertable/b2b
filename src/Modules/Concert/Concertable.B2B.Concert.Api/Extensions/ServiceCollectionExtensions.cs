using Concertable.B2B.Concert.Api.Controllers;
using Concertable.B2B.Concert.Api.Mappers;
using Concertable.B2B.Concert.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Concert.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConcertApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddConcertModule(configuration);
        services.AddSingleton<IApplicationResponseMapper, ApplicationResponseMapper>();
        services.AddSingleton<IOpportunityResponseMapper, OpportunityResponseMapper>();
        services.AddControllers()
            .AddApplicationPart(typeof(ConcertController).Assembly)
            .ConfigureApplicationPartManager(apm =>
                apm.FeatureProviders.Add(new InternalControllerFeatureProvider()));
        return services;
    }
}
