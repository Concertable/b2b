using Concertable.B2B.Deal.Infrastructure.Data.Configurations;
using Concertable.DataAccess.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Deal.Infrastructure.Data;

internal sealed class DealConfigurationProvider : IEntityTypeConfigurationProvider
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new DealEntityConfiguration());
        modelBuilder.ApplyConfiguration(new FlatFeeDealEntityConfiguration());
        modelBuilder.ApplyConfiguration(new DoorSplitDealEntityConfiguration());
        modelBuilder.ApplyConfiguration(new VersusDealEntityConfiguration());
        modelBuilder.ApplyConfiguration(new VenueHireDealEntityConfiguration());
    }
}
