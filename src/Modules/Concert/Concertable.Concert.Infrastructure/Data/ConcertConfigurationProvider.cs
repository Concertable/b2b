using Concertable.Concert.Infrastructure.Data.Configurations;
using Concertable.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Concert.Infrastructure.Data;

internal sealed class ConcertConfigurationProvider : IEntityTypeConfigurationProvider
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ConcertEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ConcertImageEntityConfiguration());
        modelBuilder.ApplyConfiguration(new BookingEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ConcertRatingProjectionConfiguration());
        modelBuilder.ApplyConfiguration(new OpportunityEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ApplicationEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ArtistReadModelConfiguration());
        modelBuilder.ApplyConfiguration(new ArtistReadModelGenreConfiguration());
        modelBuilder.ApplyConfiguration(new VenueReadModelConfiguration());
    }
}
