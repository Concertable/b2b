using Concertable.Contract.Api.Controllers;
using Concertable.Contract.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Contract.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddContractApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddContractModule(configuration);
        services.AddControllers()
            .AddApplicationPart(typeof(ContractController).Assembly)
            .ConfigureApplicationPartManager(apm =>
                apm.FeatureProviders.Add(new InternalControllerFeatureProvider()));
        return services;
    }
}
