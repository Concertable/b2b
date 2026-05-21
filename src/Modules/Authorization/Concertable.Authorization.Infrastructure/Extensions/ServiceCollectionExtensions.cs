using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Authorization.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthorizationModule(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUserAccessor>();
        return services;
    }
}
