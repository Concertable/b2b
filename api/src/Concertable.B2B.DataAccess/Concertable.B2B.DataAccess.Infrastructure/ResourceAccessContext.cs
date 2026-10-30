using Concertable.B2B.Authorization.Contracts;

namespace Concertable.B2B.DataAccess.Infrastructure;

public sealed class ResourceAccessContext : IResourceAccessContext
{
    private readonly IAuthorizationContext authorizationContext;
    private readonly TimeProvider timeProvider;

    public ResourceAccessContext(IAuthorizationContext authorizationContext, TimeProvider timeProvider)
    {
        this.authorizationContext = authorizationContext;
        this.timeProvider = timeProvider;
    }

    public MembershipSnapshot? Membership => authorizationContext.Membership;

    public DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    public ResourceCoverage CoverageFor(TenantPermission permission) => authorizationContext.CoverageFor(permission);
}
