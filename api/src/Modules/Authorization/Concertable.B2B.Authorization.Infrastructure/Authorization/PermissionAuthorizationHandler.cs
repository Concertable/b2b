using Concertable.Kernel.Identity;
using Microsoft.AspNetCore.Authorization;

namespace Concertable.B2B.Authorization.Infrastructure.Authorization;

internal sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ITenantResolver resolver;
    private readonly IMembershipContext membership;

    public PermissionAuthorizationHandler(ITenantResolver resolver, IMembershipContext membership)
    {
        this.resolver = resolver;
        this.membership = membership;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        await resolver.ResolveAsync();

        if (membership.HasPermission(requirement.Permission))
            context.Succeed(requirement);
    }
}
