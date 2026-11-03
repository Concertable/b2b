using Concertable.Kernel.Identity;
using Microsoft.AspNetCore.Authorization;

namespace Concertable.B2B.Authorization.Infrastructure.Authorization;

internal sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ITenantResolver resolver;
    private readonly IAuthorizationContext authorizationContext;

    public PermissionAuthorizationHandler(ITenantResolver resolver, IAuthorizationContext authorizationContext)
    {
        this.resolver = resolver;
        this.authorizationContext = authorizationContext;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        await resolver.ResolveAsync();

        if (authorizationContext.Membership.TryGetValue(out var actor) && actor.HasPermission(requirement.Permission))
            context.Succeed(requirement);
    }
}
