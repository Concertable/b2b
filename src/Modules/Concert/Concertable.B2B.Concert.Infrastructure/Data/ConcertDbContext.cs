using Concertable.B2B.Artist.Domain;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.ReadModels;
using Concertable.DataAccess.Infrastructure;
using Concertable.B2B.Venue.Domain;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Data;

internal sealed class ConcertDbContext(
    DbContextOptions<ConcertDbContext> options,
    ConcertConfigurationProvider provider)
    : DbContextBase(options)
{
    public DbSet<ConcertEntity> Concerts => Set<ConcertEntity>();
    public DbSet<BookingEntity> Bookings => Set<BookingEntity>();
    public DbSet<ConcertImageEntity> ConcertImages => Set<ConcertImageEntity>();
    public DbSet<OpportunityEntity> Opportunities => Set<OpportunityEntity>();
    public DbSet<ApplicationEntity> Applications => Set<ApplicationEntity>();
    public DbSet<ArtistReadModel> ArtistReadModels => Set<ArtistReadModel>();
    public DbSet<VenueReadModel> VenueReadModels => Set<VenueReadModel>();
    public DbSet<ConcertRatingProjection> ConcertRatingProjections => Set<ConcertRatingProjection>();
    public DbSet<ArtistRatingProjection> ArtistRatingProjections => Set<ArtistRatingProjection>();
    public DbSet<VenueRatingProjection> VenueRatingProjections => Set<VenueRatingProjection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema.Name);
        provider.Configure(modelBuilder);

        modelBuilder.Entity<ArtistRatingProjection>(b =>
        {
            b.ToTable("ArtistRatingProjections", "artist", t => t.ExcludeFromMigrations());
            b.HasKey(p => p.ArtistId);
            b.Property(p => p.ArtistId).ValueGeneratedNever();
        });
        modelBuilder.Entity<VenueRatingProjection>(b =>
        {
            b.ToTable("VenueRatingProjections", "venue", t => t.ExcludeFromMigrations());
            b.HasKey(p => p.VenueId);
            b.Property(p => p.VenueId).ValueGeneratedNever();
        });
    }
}
