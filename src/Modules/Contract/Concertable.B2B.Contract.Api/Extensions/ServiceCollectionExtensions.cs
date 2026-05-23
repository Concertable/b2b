using Concertable.B2B.Contract.Api.Controllers;
using Concertable.B2B.Contract.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Contract.Api.Extensions;

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
