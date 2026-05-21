using Concertable.Messaging.Domain;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Venue.Infrastructure.Data;

internal class VenueDbContext(
    DbContextOptions<VenueDbContext> options,
    VenueConfigurationProvider provider)
    : DbContextBase(options)
{
    public DbSet<VenueEntity> Venues => Set<VenueEntity>();
    public DbSet<VenueImageEntity> VenueImages => Set<VenueImageEntity>();
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
    }
}
