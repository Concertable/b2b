using Concertable.DataAccess.Infrastructure;
using Concertable.Venue.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Venue.Infrastructure.Data;

internal sealed class VenueConfigurationProvider : IEntityTypeConfigurationProvider
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new VenueEntityConfiguration());
        modelBuilder.ApplyConfiguration(new VenueRatingProjectionConfiguration());
    }
}
