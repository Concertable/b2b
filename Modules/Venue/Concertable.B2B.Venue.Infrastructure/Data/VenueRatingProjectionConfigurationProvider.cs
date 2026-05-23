using Concertable.DataAccess.Infrastructure.Data;
using Concertable.B2B.Venue.Domain;
using Concertable.B2B.Venue.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Venue.Infrastructure.Data;

internal sealed class VenueRatingProjectionConfigurationProvider : IRatingProjectionConfigurationProvider
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new VenueRatingProjectionConfiguration());
        modelBuilder.Entity<VenueRatingProjection>().ToTable(t => t.ExcludeFromMigrations());
    }
}
