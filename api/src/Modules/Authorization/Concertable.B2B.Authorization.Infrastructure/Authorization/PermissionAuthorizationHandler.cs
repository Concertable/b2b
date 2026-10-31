using Concertable.Kernel.Identity;
using Microsoft.AspNetCore.Authorization;

namespace Concertable.B2B.Authorization.Infrastructure.Authorization;

internal sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ITenantResolver resolver;
    private readonly IAuthorizationContext authorization;

    public PermissionAuthorizationHandler(ITenantResolver resolver, IAuthorizationContext authorization)
    {
        this.resolver = resolver;
        this.authorization = authorization;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        await resolver.ResolveAsync();

        if (authorization.Membership?.HasPermission(requirement.Permission) is true)
            context.Succeed(requirement);
    }
}
