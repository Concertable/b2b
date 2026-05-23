using Concertable.DataAccess.Infrastructure.Data;
using Concertable.Venue.Domain;
using Concertable.Venue.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Venue.Infrastructure.Data;

internal sealed class VenueRatingProjectionConfigurationProvider : IRatingProjectionConfigurationProvider
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new VenueRatingProjectionConfiguration());
        modelBuilder.Entity<VenueRatingProjection>().ToTable(t => t.ExcludeFromMigrations());
    }
}
