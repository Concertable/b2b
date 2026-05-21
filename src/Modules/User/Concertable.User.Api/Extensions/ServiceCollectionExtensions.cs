using Concertable.User.Api.Controllers;
using Concertable.User.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.User.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUserApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddUserModule(configuration);
        services.AddControllers()
            .AddApplicationPart(typeof(UserController).Assembly)
            .ConfigureApplicationPartManager(apm =>
                apm.FeatureProviders.Add(new InternalControllerFeatureProvider()));
        return services;
    }
}
