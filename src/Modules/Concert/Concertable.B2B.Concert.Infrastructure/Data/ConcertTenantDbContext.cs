using Concertable.B2B.Artist.Domain.ReadModels;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.ReadModels;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Venue.Domain.ReadModels;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Data;

internal sealed class ConcertTenantDbContext(
    DbContextOptions<ConcertTenantDbContext> options,
    ConcertConfigurationProvider provider,
    ITenantContext tenantContext)
    : VenueArtistTenantDbContext(options, provider, tenantContext, Schema.Name)
{
    public DbSet<ConcertEntity> Concerts => Set<ConcertEntity>();
    public DbSet<BookingEntity> Bookings => Set<BookingEntity>();
    public DbSet<ContractEntity> Contracts => Set<ContractEntity>();
    public DbSet<InvoiceEntity> Invoices => Set<InvoiceEntity>();
    public DbSet<InvoiceSequenceEntity> InvoiceSequences => Set<InvoiceSequenceEntity>();
    public DbSet<SelfBillingAgreementEntity> SelfBillingAgreements => Set<SelfBillingAgreementEntity>();
    public DbSet<ConcertImageEntity> ConcertImages => Set<ConcertImageEntity>();
    public DbSet<OpportunityEntity> Opportunities => Set<OpportunityEntity>();
    public DbSet<ApplicationEntity> Applications => Set<ApplicationEntity>();
    public DbSet<ArtistReadModel> ArtistReadModels => Set<ArtistReadModel>();
    public DbSet<VenueReadModel> VenueReadModels => Set<VenueReadModel>();
    public DbSet<ConcertRatingProjection> ConcertRatingProjections => Set<ConcertRatingProjection>();
    public DbSet<ArtistRatingProjection> ArtistRatingProjections => Set<ArtistRatingProjection>();
    public DbSet<VenueRatingProjection> VenueRatingProjections => Set<VenueRatingProjection>();

    protected override void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyVenueArtist<ApplicationEntity>(this);
        modelBuilder.ApplyVenueArtist<BookingEntity>(this);
        modelBuilder.ApplyVenueArtist<ContractEntity>(this);
        modelBuilder.ApplyVenueArtist<InvoiceEntity>(this);
        modelBuilder.ApplySingleOwner<SelfBillingAgreementEntity>(this);
    }
}
