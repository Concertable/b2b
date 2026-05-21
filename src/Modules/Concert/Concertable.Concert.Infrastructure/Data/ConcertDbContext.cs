using Concertable.Artist.Domain;
using Concertable.Concert.Domain;
using Concertable.DataAccess.Infrastructure;
using Concertable.Messaging.Domain;
using Concertable.Venue.Domain;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Concert.Infrastructure.Data;

internal class ConcertDbContext(
    DbContextOptions<ConcertDbContext> options,
    ConcertConfigurationProvider provider)
    : DbContextBase(options)
{
    public DbSet<ConcertEntity> Concerts => Set<ConcertEntity>();
    public DbSet<BookingEntity> Bookings => Set<BookingEntity>();
    public DbSet<ConcertGenreEntity> ConcertGenres => Set<ConcertGenreEntity>();
    public DbSet<ConcertImageEntity> ConcertImages => Set<ConcertImageEntity>();
    public DbSet<OpportunityEntity> Opportunities => Set<OpportunityEntity>();
    public DbSet<ApplicationEntity> Applications => Set<ApplicationEntity>();
    public DbSet<OpportunityGenreEntity> OpportunityGenres => Set<OpportunityGenreEntity>();
    public DbSet<ArtistReadModel> ArtistReadModels => Set<ArtistReadModel>();
    public DbSet<VenueReadModel> VenueReadModels => Set<VenueReadModel>();
    public DbSet<ConcertRatingProjection> ConcertRatingProjections => Set<ConcertRatingProjection>();
    public DbSet<ArtistRatingProjection> ArtistRatingProjections => Set<ArtistRatingProjection>();
    public DbSet<VenueRatingProjection> VenueRatingProjections => Set<VenueRatingProjection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema.Name);

        provider.Configure(modelBuilder);

        modelBuilder.Entity<OutboxMessageEntity>(b =>
        {
            b.ToTable("Outbox", "messaging", t => t.ExcludeFromMigrations());
            b.Property(m => m.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<InboxMessageEntity>(b =>
        {
            b.ToTable("Inbox", "messaging", t => t.ExcludeFromMigrations());
            b.HasKey(m => new { m.MessageId, m.ConsumerName });
            b.Property(m => m.MessageId).ValueGeneratedNever();
            b.Property(m => m.ConsumerName).IsRequired().HasMaxLength(256);
            b.Property(m => m.MessageType).IsRequired().HasColumnType("nvarchar(450)");
            b.Property(m => m.ReceivedAt).IsRequired();
        });

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
