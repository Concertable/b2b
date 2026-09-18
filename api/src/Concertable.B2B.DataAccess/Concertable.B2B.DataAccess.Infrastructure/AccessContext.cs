using Concertable.B2B.Authorization.Contracts;
using Concertable.Kernel.Identity;

namespace Concertable.B2B.DataAccess.Infrastructure;

public sealed class AccessContext : IAccessContext
{
    private readonly ITenantContext tenantContext;
    private readonly IMembershipContext membershipContext;
    private readonly TimeProvider timeProvider;

    public AccessContext(
        ITenantContext tenantContext,
        IMembershipContext membershipContext,
        TimeProvider timeProvider)
    {
        this.tenantContext = tenantContext;
        this.membershipContext = membershipContext;
        this.timeProvider = timeProvider;
    }

    public Guid? TenantId => tenantContext.TenantId;

    public Guid? UserId => membershipContext.UserId;

    public long? AuthorizationVersion => membershipContext.AuthorizationVersion;

    public bool IsHost => tenantContext.IsHost;

    public DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;
}
