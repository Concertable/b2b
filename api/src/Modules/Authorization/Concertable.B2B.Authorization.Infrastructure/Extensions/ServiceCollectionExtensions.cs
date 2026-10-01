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
        services.AddSingleton<IMembershipContextAccessor, MembershipContextAccessor>();

        services.AddScoped<MembershipContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<MembershipContext>());
        services.AddScoped<ITenantResolver>(sp => sp.GetRequiredService<MembershipContext>());
        services.AddScoped<IMembershipContext>(sp => sp.GetRequiredService<MembershipContext>());
        services.AddScoped<ActorAuthoritySession>();
        services.AddScoped(provider => new TenantCapabilityRegistry(AuthorizationCatalog.Permissions.Values));
        services.AddScoped<ITenantCapabilityAuthorization, TenantCapabilityAuthorization>();

        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }

    public static IServiceCollection AddResourceAuthorization(this IServiceCollection services)
    {
        services.AddScoped(provider => new ResourceBindingRegistry(
            AuthorizationCatalog.Permissions.Values,
            provider.GetServices<IResourceAuthorizationEvaluator>()));
        services.AddScoped<IResourceAuthorization, ResourceAuthorization>();
        return services;
    }

}
