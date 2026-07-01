using Concertable.B2B.Contract.Infrastructure.Data.Configurations;
using Concertable.DataAccess.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Contract.Infrastructure.Data;

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
