using Concertable.B2B.Conversations.Api.Controllers;
using Concertable.B2B.Conversations.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Conversations.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConversationsApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddConversationsModule(configuration);
        services.AddControllers()
            .AddApplicationPart(typeof(MessageController).Assembly)
            .ConfigureApplicationPartManager(apm =>
                apm.FeatureProviders.Add(new InternalControllerFeatureProvider()));
        return services;
    }
}
