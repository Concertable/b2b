using Concertable.B2B.Artist.Domain.ReadModels;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Concert.Contracts.Enums;
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
    IResourceAccessContext resourceAccess)
    : ResourceScopedDbContext(options, outboxOptions, provider, tenantContext, resourceAccess, Schema.Name)
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
    public DbSet<ConcertCommandReceipt> ConcertCommandReceipts => Set<ConcertCommandReceipt>();

    public ResourceAudience OperationsAudience => AudienceFor(TenantPermission.OperationsView);
    public ResourceAudience FinanceAudience => AudienceFor(TenantPermission.SettlementView);

    protected override void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConcertAccessGrant>().HasQueryFilter(TenantFilters.Key,
            ResourceAccessExpressions.LiveForAudience<ConcertAccessGrant, ConcertAccessScope>(
                    this,
                    _ => OperationsAudience,
                    ConcertAccessScope.Summary,
                    ConcertAccessScope.Operations)
                .Or(ResourceAccessExpressions.LiveForAudience<ConcertAccessGrant, ConcertAccessScope>(
                    this,
                    _ => FinanceAudience,
                    ConcertAccessScope.Finance)));

        modelBuilder.Entity<InvoiceAccessGrant>().HasQueryFilter(TenantFilters.Key,
            ResourceAccessExpressions.LiveForAudience<InvoiceAccessGrant, InvoiceAccessScope>(
                this,
                _ => FinanceAudience,
                InvoiceAccessScope.Read));

        modelBuilder.Entity<ConcertEntity>().HasQueryFilter(TenantFilters.Key, concert =>
            ConcertAccessGrants.Any(grant =>
                grant.ResourceId == concert.Id && grant.Scope == ConcertAccessScope.Summary));

        modelBuilder.Entity<InvoiceEntity>().HasQueryFilter(TenantFilters.Key, invoice =>
            InvoiceAccessGrants.Any(grant =>
                grant.ResourceId == invoice.Id && grant.Scope == InvoiceAccessScope.Read));

        modelBuilder.ApplySingleOwner<SelfBillingAgreementEntity>(this);
    }
}
