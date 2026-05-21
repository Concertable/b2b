using Concertable.Messaging.Domain;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Artist.Infrastructure.Data;

internal class ArtistDbContext(
    DbContextOptions<ArtistDbContext> options,
    ArtistConfigurationProvider provider)
    : DbContextBase(options)
{
    public DbSet<ArtistEntity> Artists => Set<ArtistEntity>();
    public DbSet<ArtistGenreEntity> ArtistGenres => Set<ArtistGenreEntity>();
    public DbSet<ArtistRatingProjection> ArtistRatingProjections => Set<ArtistRatingProjection>();

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
