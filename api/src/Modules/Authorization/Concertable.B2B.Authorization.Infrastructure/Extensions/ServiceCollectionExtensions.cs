using Concertable.B2B.Authorization.Infrastructure.Authorization;
using Concertable.B2B.Authorization.Infrastructure.Services;
using Concertable.Kernel.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Authorization.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers request authority. The module owns no storage: the host must also bind
    /// <see cref="IMembershipReadRepository"/> to the module that owns membership rows.
    /// </summary>
    public static IServiceCollection AddAuthorizationModule(this IServiceCollection services)
    {
        services.AddSingleton<ExecutionScope>();
        services.AddSingleton<IExecutionScope>(sp => sp.GetRequiredService<ExecutionScope>());
        services.AddSingleton<IExecutionScopeActivator>(sp => sp.GetRequiredService<ExecutionScope>());

        services.AddSingleton<IMembershipContextAccessor, MembershipContextAccessor>();
        services.AddSingleton<IPermissionCatalog, PermissionCatalog>();

        services.AddScoped<MembershipContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<MembershipContext>());
        services.AddScoped<ITenantResolver>(sp => sp.GetRequiredService<MembershipContext>());
        services.AddScoped<IMembershipContext>(sp => sp.GetRequiredService<MembershipContext>());

        /* String-permission authorization: a single on-demand policy provider (singleton) builds every
           perm:<name> policy and delegates Admin/[Authorize] to the default provider; the scoped handler
           reads the membership context. No startup policy loop. */
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }
}
