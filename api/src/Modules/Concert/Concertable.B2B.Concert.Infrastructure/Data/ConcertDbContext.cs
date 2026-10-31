using Concertable.B2B.Artist.Domain.ReadModels;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.ReadModels;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Venue.Domain.ReadModels;
using Concertable.Kernel.Identity;
using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Concert.Infrastructure.Data;

internal sealed class ConcertDbContext(
    DbContextOptions<ConcertDbContext> options,
    IOptions<OutboxOptions> outboxOptions,
    ConcertConfigurationProvider provider,
    ITenantContext tenantContext,
    IAuthorizationContext authorization,
    TimeProvider timeProvider)
    : ResourceScopedDbContext(options, outboxOptions, provider, tenantContext, authorization, timeProvider, Schema.Name)
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
    public DbSet<ConcertCommandReceipt> ConcertCommandReceipts => Set<ConcertCommandReceipt>();

    public ResourceAccess OperationsAccess => ActorAccess(TenantPermission.OperationsView);
    public ResourceAccess FinanceAccess => ActorAccess(TenantPermission.SettlementView);

    protected override void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConcertEntity>().HasQueryFilter(TenantFilters.Key, concert =>
            OperationsAccess == ResourceAccess.All
                && (concert.VenueTenantId == ActorTenantId
                    || concert.ArtistTenantId == ActorTenantId
                    || concert.Shares.Any(share =>
                        share.RecipientTenantId == ActorTenantId
                        && share.RecipientMembershipId == null
                        && (share.ExpiresAt == null || share.ExpiresAt > Now)))
            || OperationsAccess != ResourceAccess.None
                && (concert.Assignments.Any(assignment => assignment.MembershipId == ActorMembershipId)
                    || concert.Shares.Any(share =>
                        share.RecipientTenantId == ActorTenantId
                        && share.RecipientMembershipId == ActorMembershipId
                        && (share.ExpiresAt == null || share.ExpiresAt > Now))));

        modelBuilder.Entity<InvoiceEntity>().HasQueryFilter(TenantFilters.Key, invoice =>
            FinanceAccess == ResourceAccess.All
            && (invoice.VenueTenantId == ActorTenantId || invoice.ArtistTenantId == ActorTenantId));

        modelBuilder.ApplySingleOwner<SelfBillingAgreementEntity>(this);
    }
}
