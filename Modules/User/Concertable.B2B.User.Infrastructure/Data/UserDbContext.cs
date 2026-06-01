using Concertable.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.User.Infrastructure.Data;

internal sealed class UserDbContext(
    DbContextOptions<UserDbContext> options,
    UserConfigurationProvider provider)
    : DbContextBase(options)
{
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<VenueManagerProfileEntity> VenueManagerProfiles => Set<VenueManagerProfileEntity>();
    public DbSet<ArtistManagerProfileEntity> ArtistManagerProfiles => Set<ArtistManagerProfileEntity>();
    public DbSet<AdminProfileEntity> AdminProfiles => Set<AdminProfileEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema.Name);
        provider.Configure(modelBuilder);
    }
}
