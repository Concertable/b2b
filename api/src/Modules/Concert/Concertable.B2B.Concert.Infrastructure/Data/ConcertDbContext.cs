using Concertable.B2B.Artist.Domain.ReadModels;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.ReadModels;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Venue.Domain.ReadModels;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Data;

internal sealed class ConcertDbContext(
    DbContextOptions<ConcertDbContext> options,
    ConcertConfigurationProvider provider,
    ITenantContext tenantContext,
    IAccessContext accessContext)
    : AccessScopedDbContext(options, provider, tenantContext, accessContext, Schema.Name)
{
    public DbSet<ConcertEntity> Concerts => Set<ConcertEntity>();
    public DbSet<InvoiceEntity> Invoices => Set<InvoiceEntity>();
    public DbSet<InvoiceSequenceEntity> InvoiceSequences => Set<InvoiceSequenceEntity>();
    public DbSet<SelfBillingAgreementEntity> SelfBillingAgreements => Set<SelfBillingAgreementEntity>();
    public DbSet<ConcertImageEntity> ConcertImages => Set<ConcertImageEntity>();
    public DbSet<ArtistReadModel> ArtistReadModels => Set<ArtistReadModel>();
    public DbSet<VenueReadModel> VenueReadModels => Set<VenueReadModel>();
    public DbSet<ConcertRatingProjection> ConcertRatingProjections => Set<ConcertRatingProjection>();
    public DbSet<ArtistRatingProjection> ArtistRatingProjections => Set<ArtistRatingProjection>();
    public DbSet<VenueRatingProjection> VenueRatingProjections => Set<VenueRatingProjection>();
    public DbSet<ConcertAccessGrant> ConcertAccessGrants => Set<ConcertAccessGrant>();
    public DbSet<InvoiceAccessGrant> InvoiceAccessGrants => Set<InvoiceAccessGrant>();

    /* Each filter is written out against its own grant set rather than derived from a marker: the predicate
       has to name the grant family it reads. Every reference is to the context instance, which EF re-binds
       per query. */
    protected override void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConcertEntity>().HasQueryFilter(TenantFilters.Key, concert =>
            AccessContext.IsHost
            || (AccessContext.UserId != null
                && MembershipAuthority.Any(membership =>
                    membership.TenantId == AccessContext.TenantId
                    && membership.UserId == AccessContext.UserId
                    && membership.AuthorizationVersion == AccessContext.AuthorizationVersion)
                && ConcertAccessGrants.Any(grant =>
                    grant.ResourceId == concert.Id
                    && grant.TenantId == AccessContext.TenantId
                    && (grant.MemberUserId == null || grant.MemberUserId == AccessContext.UserId)
                    && grant.RevokedAt == null
                    && grant.ValidFrom <= AccessContext.UtcNow
                    && (grant.ValidUntil == null || grant.ValidUntil > AccessContext.UtcNow))));

        modelBuilder.Entity<InvoiceEntity>().HasQueryFilter(TenantFilters.Key, invoice =>
            AccessContext.IsHost
            || (AccessContext.UserId != null
                && MembershipAuthority.Any(membership =>
                    membership.TenantId == AccessContext.TenantId
                    && membership.UserId == AccessContext.UserId
                    && membership.AuthorizationVersion == AccessContext.AuthorizationVersion)
                && InvoiceAccessGrants.Any(grant =>
                    grant.ResourceId == invoice.Id
                    && grant.TenantId == AccessContext.TenantId
                    && (grant.MemberUserId == null || grant.MemberUserId == AccessContext.UserId)
                    && grant.RevokedAt == null
                    && grant.ValidFrom <= AccessContext.UtcNow
                    && (grant.ValidUntil == null || grant.ValidUntil > AccessContext.UtcNow))));

        modelBuilder.ApplySingleOwner<SelfBillingAgreementEntity>(this);
    }
}
