using Concertable.B2B.Authorization.Infrastructure.Authorization;
using Concertable.B2B.Authorization.Infrastructure.Services;
using Concertable.Kernel.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Authorization.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthorizationModule(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationContextAccessor, AuthorizationContextAccessor>();

        services.AddScoped<AuthorizationContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<AuthorizationContext>());
        services.AddScoped<ITenantResolver>(sp => sp.GetRequiredService<AuthorizationContext>());
        services.AddScoped<IAuthorizationContext>(sp => sp.GetRequiredService<AuthorizationContext>());

        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }

}
