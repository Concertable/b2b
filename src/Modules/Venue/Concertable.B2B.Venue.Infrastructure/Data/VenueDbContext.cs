using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Venue.Infrastructure.Data;

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
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema.Name);
        provider.Configure(modelBuilder);
    }
}
