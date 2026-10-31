using System.Linq.Expressions;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.DataAccess.Application;
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

    // Must stay a DbSet: an IQueryable member is parameterised whole and its Any then cannot translate.
    public DbSet<MembershipAuthority> MembershipAuthority => Set<MembershipAuthority>();

    public MembershipSnapshot? Membership => authorization.Membership;

    public DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    // A filter cannot dereference Membership — the member access is evaluated before the query runs, so an
    // unresolved caller would throw rather than read nothing.
    public Guid? ActiveMembershipId => Membership?.MembershipId;
    public Guid? ActiveTenantId => Membership?.TenantId;
    public Guid? ActiveUserId => Membership?.UserId;
    public long? ActivePermissionVersion => Membership?.PermissionVersion;
    public long? ActiveRolePolicyVersion => Membership?.RolePolicyVersion;

    protected Expression<Func<TGrant, bool>> IsVisibleGrant<TGrant, TScope>(
        Expression<Func<ResourceAccess>> access, params TScope[] scopes)
        where TGrant : ResourceAccessGrant<TScope>
        where TScope : struct, Enum
    {
        Expression<Func<TGrant, bool>> currentMember = grant =>
            ActiveMembershipId != null
            && MembershipAuthority.Any(member =>
                member.MembershipId == ActiveMembershipId
                && member.TenantId == ActiveTenantId
                && member.UserId == ActiveUserId
                && member.PermissionVersion == ActivePermissionVersion
                && member.RolePolicyVersion == ActiveRolePolicyVersion);

        return currentMember.And(ResourceAccessGrant<TScope>.IsLiveFor<TGrant>(
            () => ActiveTenantId,
            () => ActiveMembershipId,
            access,
            () => UtcNow,
            scopes));
    }

    protected override void ConfigureMembershipAuthority(ModelBuilder modelBuilder)
    {
        base.ConfigureMembershipAuthority(modelBuilder);
        modelBuilder.ApplyConfiguration(new MembershipAuthorityConfiguration());
    }
}
