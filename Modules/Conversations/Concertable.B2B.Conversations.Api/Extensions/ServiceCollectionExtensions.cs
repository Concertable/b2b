using Concertable.B2B.Conversations.Api.Controllers;
using Concertable.B2B.Conversations.Infrastructure.Extensions;
using Concertable.Shared.Api.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Conversations.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConversationsApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddConversationsModule(configuration);
        services.AddControllers()
            .AddInternalControllers(typeof(MessageController).Assembly);
        return services;
    }
}
