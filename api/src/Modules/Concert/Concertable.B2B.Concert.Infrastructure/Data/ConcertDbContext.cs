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
    public DbSet<ConcertAccessGrant> ConcertAccessGrants => Set<ConcertAccessGrant>();
    public DbSet<InvoiceAccessGrant> InvoiceAccessGrants => Set<InvoiceAccessGrant>();
    public DbSet<ConcertCommandReceipt> ConcertCommandReceipts => Set<ConcertCommandReceipt>();

    public ResourceAccess OperationsAccess =>
        Membership?.Permissions[TenantPermission.OperationsView] ?? ResourceAccess.None;
    public ResourceAccess FinanceAccess =>
        Membership?.Permissions[TenantPermission.SettlementView] ?? ResourceAccess.None;

    protected override void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConcertAccessGrant>().HasQueryFilter(TenantFilters.Key,
            IsVisibleGrant<ConcertAccessGrant, ConcertAccessScope>(
                    () => OperationsAccess,
                    ConcertAccessScope.Summary,
                    ConcertAccessScope.Operations)
                .Or(IsVisibleGrant<ConcertAccessGrant, ConcertAccessScope>(
                    () => FinanceAccess,
                    ConcertAccessScope.Finance)));

        modelBuilder.Entity<InvoiceAccessGrant>().HasQueryFilter(TenantFilters.Key,
            IsVisibleGrant<InvoiceAccessGrant, InvoiceAccessScope>(
                () => FinanceAccess,
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
