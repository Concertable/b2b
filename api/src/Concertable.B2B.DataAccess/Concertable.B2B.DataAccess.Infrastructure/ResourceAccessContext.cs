using Concertable.B2B.Authorization.Contracts;

namespace Concertable.B2B.DataAccess.Infrastructure;

public sealed class ResourceAccessContext : IResourceAccessContext
{
    private readonly IMembershipContext membershipContext;
    private readonly TimeProvider timeProvider;

    public ResourceAccessContext(IMembershipContext membershipContext, TimeProvider timeProvider)
    {
        this.membershipContext = membershipContext;
        this.timeProvider = timeProvider;
    }

    public MembershipSnapshot? Membership => membershipContext.Membership;

    public DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    public ResourceAudience AudienceFor(TenantPermission permission) => membershipContext.AudienceFor(permission);
}
