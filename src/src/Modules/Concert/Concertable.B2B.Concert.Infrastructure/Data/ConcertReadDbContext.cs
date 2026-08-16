using Concertable.B2B.Artist.Domain.ReadModels;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.ReadModels;
using Concertable.B2B.Venue.Domain.ReadModels;
using Concertable.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Data;

internal sealed class ConcertReadDbContext(
    DbContextOptions<ConcertReadDbContext> options,
    ConcertConfigurationProvider provider)
    : ReadDbContext(options, provider, Schema.Name)
{
    public DbSet<ConcertEntity> Concerts => Set<ConcertEntity>();
    public DbSet<BookingEntity> Bookings => Set<BookingEntity>();
    public DbSet<OpportunityEntity> Opportunities => Set<OpportunityEntity>();
    public DbSet<SelfBillingAgreementEntity> SelfBillingAgreements => Set<SelfBillingAgreementEntity>();
    public DbSet<ConcertRatingProjection> ConcertRatingProjections => Set<ConcertRatingProjection>();
    public DbSet<ArtistRatingProjection> ArtistRatingProjections => Set<ArtistRatingProjection>();
    public DbSet<VenueRatingProjection> VenueRatingProjections => Set<VenueRatingProjection>();
}
