using Concertable.Contract.Infrastructure.Data.Configurations;
using Concertable.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Contract.Infrastructure.Data;

internal sealed class ContractConfigurationProvider : IEntityTypeConfigurationProvider
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ContractEntityConfiguration());
        modelBuilder.ApplyConfiguration(new FlatFeeContractEntityConfiguration());
        modelBuilder.ApplyConfiguration(new DoorSplitContractEntityConfiguration());
        modelBuilder.ApplyConfiguration(new VersusContractEntityConfiguration());
        modelBuilder.ApplyConfiguration(new VenueHireContractEntityConfiguration());
    }
}
