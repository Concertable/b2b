using Concertable.B2B.Artist.Domain;
using Concertable.B2B.Artist.Infrastructure.Data.Configurations;
using Concertable.DataAccess.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Artist.Infrastructure.Data;

internal sealed class ArtistRatingProjectionConfigurationProvider : IRatingProjectionConfigurationProvider
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ArtistRatingProjectionConfiguration());
        modelBuilder.Entity<ArtistRatingProjection>().ToTable(t => t.ExcludeFromMigrations());
    }
}
