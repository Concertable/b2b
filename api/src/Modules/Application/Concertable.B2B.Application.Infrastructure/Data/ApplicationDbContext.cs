using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Application.Infrastructure.Data;

internal sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    ApplicationConfigurationProvider provider,
    ITenantContext tenantContext,
    IAccessContext accessContext)
    : AccessScopedDbContext(options, provider, tenantContext, accessContext, Schema.Name)
{
    public DbSet<ApplicationEntity> Applications => Set<ApplicationEntity>();
    public DbSet<VerifyPaymentEntity> VerifyPayments => Set<VerifyPaymentEntity>();
    public DbSet<ConcertAvailabilityEntity> ConcertAvailabilities => Set<ConcertAvailabilityEntity>();
    public DbSet<ApplicationAccessGrant> ApplicationAccessGrants => Set<ApplicationAccessGrant>();

    /* The availability projection is deliberately unfiltered: it answers whether a date is taken and
       nothing else, so there is nothing tenant-private in it to protect. */
    protected override void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApplicationEntity>().HasQueryFilter(TenantFilters.Key, application =>
            AccessContext.IsHost
            || (AccessContext.UserId != null
                && MembershipAuthority.Any(membership =>
                    membership.TenantId == AccessContext.TenantId
                    && membership.UserId == AccessContext.UserId
                    && membership.AuthorizationVersion == AccessContext.AuthorizationVersion)
                && ApplicationAccessGrants.Any(grant =>
                    grant.ResourceId == application.Id
                    && grant.TenantId == AccessContext.TenantId
                    && (grant.MemberUserId == null || grant.MemberUserId == AccessContext.UserId)
                    && grant.RevokedAt == null
                    && grant.ValidFrom <= AccessContext.UtcNow
                    && (grant.ValidUntil == null || grant.ValidUntil > AccessContext.UtcNow))));
    }
}
