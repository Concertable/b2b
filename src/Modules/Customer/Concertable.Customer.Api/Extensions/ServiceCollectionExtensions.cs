using Concertable.Customer.Api.Controllers;
using Concertable.Customer.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Customer.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCustomerApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCustomerModule(configuration);
        services.AddControllers()
            .AddApplicationPart(typeof(PreferenceController).Assembly)
            .ConfigureApplicationPartManager(apm =>
                apm.FeatureProviders.Add(new InternalControllerFeatureProvider()));
        return services;
    }
}
