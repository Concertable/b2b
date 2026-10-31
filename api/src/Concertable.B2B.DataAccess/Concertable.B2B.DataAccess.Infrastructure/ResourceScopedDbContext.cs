using Concertable.B2B.Authorization.Contracts;
using Concertable.DataAccess.Infrastructure.Data;
using Concertable.Kernel.Identity;
using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.DataAccess.Infrastructure;

public abstract class ResourceScopedDbContext : TenantScopedDbContext
{
    private readonly IAuthorizationContext authorization;
    private readonly TimeProvider timeProvider;

    protected ResourceScopedDbContext(
        DbContextOptions options,
        IOptions<OutboxOptions> outboxOptions,
        IEntityTypeConfigurationProvider provider,
        ITenantContext tenantContext,
        IAuthorizationContext authorization,
        TimeProvider timeProvider,
        string defaultSchema)
        : base(options, outboxOptions, provider, tenantContext, defaultSchema)
    {
        this.authorization = authorization;
        this.timeProvider = timeProvider;
    }

    public MembershipSnapshot? Actor => authorization.Membership;

    public DateTime Now => timeProvider.GetUtcNow().UtcDateTime;

    // A filter cannot dereference Actor — the member access is evaluated before the query runs, so an
    // unresolved caller would throw rather than read nothing.
    public Guid? ActorTenantId => Actor?.TenantId;
    public Guid? ActorMembershipId => Actor?.MembershipId;
    public Guid? ActorUserId => Actor?.UserId;

    protected ResourceAccess ActorAccess(TenantPermission permission) =>
        Actor?.Permissions[permission] ?? ResourceAccess.None;
}
