using Microsoft.EntityFrameworkCore;

namespace Concertable.Contract.Infrastructure.Data;

internal class ContractDbContext(
    DbContextOptions<ContractDbContext> options,
    ContractConfigurationProvider provider)
    : DbContextBase(options)
{
    public DbSet<ContractEntity> Contracts => Set<ContractEntity>();
    public DbSet<FlatFeeContractEntity> FlatFeeContracts => Set<FlatFeeContractEntity>();
    public DbSet<DoorSplitContractEntity> DoorSplitContracts => Set<DoorSplitContractEntity>();
    public DbSet<VersusContractEntity> VersusContracts => Set<VersusContractEntity>();
    public DbSet<VenueHireContractEntity> VenueHireContracts => Set<VenueHireContractEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema.Name);

        provider.Configure(modelBuilder);
    }
}
