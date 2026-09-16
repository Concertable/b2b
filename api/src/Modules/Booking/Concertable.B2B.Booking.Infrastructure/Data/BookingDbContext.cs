using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Booking.Infrastructure.Data;

internal sealed class BookingDbContext(
    DbContextOptions<BookingDbContext> options,
    BookingConfigurationProvider provider,
    ITenantContext tenantContext,
    IAccessContext accessContext)
    : AccessScopedDbContext(options, provider, tenantContext, accessContext, Schema.Name)
{
    public DbSet<BookingEntity> Bookings => Set<BookingEntity>();
    public DbSet<ContractEntity> Contracts => Set<ContractEntity>();
    public DbSet<BookingAccessGrant> BookingAccessGrants => Set<BookingAccessGrant>();
    public DbSet<ContractAccessGrant> ContractAccessGrants => Set<ContractAccessGrant>();

    /* Each filter is written out against its own grant set rather than derived from a marker: the predicate
       has to name the grant family it reads, and which resources are reached by grant at all is a per-entity
       product decision. Every reference is to the context instance, which EF re-binds per query. */
    protected override void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BookingEntity>().HasQueryFilter(TenantFilters.Key, booking =>
            AccessContext.IsHost
            || (AccessContext.UserId != null
                && MembershipAuthority.Any(membership =>
                    membership.TenantId == AccessContext.TenantId
                    && membership.UserId == AccessContext.UserId
                    && membership.AuthorizationVersion == AccessContext.AuthorizationVersion)
                && BookingAccessGrants.Any(grant =>
                    grant.ResourceId == booking.Id
                    && grant.TenantId == AccessContext.TenantId
                    && (grant.MemberUserId == null || grant.MemberUserId == AccessContext.UserId)
                    && grant.RevokedAt == null
                    && grant.ValidFrom <= AccessContext.UtcNow
                    && (grant.ValidUntil == null || grant.ValidUntil > AccessContext.UtcNow))));

        modelBuilder.Entity<ContractEntity>().HasQueryFilter(TenantFilters.Key, contract =>
            AccessContext.IsHost
            || (AccessContext.UserId != null
                && MembershipAuthority.Any(membership =>
                    membership.TenantId == AccessContext.TenantId
                    && membership.UserId == AccessContext.UserId
                    && membership.AuthorizationVersion == AccessContext.AuthorizationVersion)
                && ContractAccessGrants.Any(grant =>
                    grant.ResourceId == contract.Id
                    && grant.TenantId == AccessContext.TenantId
                    && (grant.MemberUserId == null || grant.MemberUserId == AccessContext.UserId)
                    && grant.RevokedAt == null
                    && grant.ValidFrom <= AccessContext.UtcNow
                    && (grant.ValidUntil == null || grant.ValidUntil > AccessContext.UtcNow))));
    }
}
