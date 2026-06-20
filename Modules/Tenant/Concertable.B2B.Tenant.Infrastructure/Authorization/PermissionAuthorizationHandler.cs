using Concertable.B2B.Tenant.Contracts;
using Concertable.Kernel.Identity;
using Microsoft.AspNetCore.Authorization;

namespace Concertable.B2B.Tenant.Infrastructure.Authorization;

/// <summary>
/// Reads authority from the DB membership row, not the token — so role changes and removals take effect on
/// the very next request. Does no query of its own: it triggers the memoized tenant resolution (safe whether
/// or not <c>TenantResolutionMiddleware</c> ran first) then asks the request-scoped
/// <see cref="IMembershipContext"/>. Registered scoped, matching the membership context it depends on.
/// </summary>
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

        if (membership.HasPermission(requirement.Permission, requirement.Persona))
            context.Succeed(requirement);
    }
}
