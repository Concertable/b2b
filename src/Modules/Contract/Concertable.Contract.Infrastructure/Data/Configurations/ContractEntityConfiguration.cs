using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.Contract.Infrastructure.Data.Configurations;

internal class ContractEntityConfiguration : IEntityTypeConfiguration<ContractEntity>
{
    public void Configure(EntityTypeBuilder<ContractEntity> builder)
    {
        builder.ToTable("Contracts", Schema.Name);
        builder.UseTptMappingStrategy();
    }
}

internal class FlatFeeContractEntityConfiguration : IEntityTypeConfiguration<FlatFeeContractEntity>
{
    public void Configure(EntityTypeBuilder<FlatFeeContractEntity> builder)
        => builder.ToTable("FlatFeeContracts", Schema.Name);
}

internal class DoorSplitContractEntityConfiguration : IEntityTypeConfiguration<DoorSplitContractEntity>
{
    public void Configure(EntityTypeBuilder<DoorSplitContractEntity> builder)
        => builder.ToTable("DoorSplitContracts", Schema.Name);
}

internal class VersusContractEntityConfiguration : IEntityTypeConfiguration<VersusContractEntity>
{
    public void Configure(EntityTypeBuilder<VersusContractEntity> builder)
        => builder.ToTable("VersusContracts", Schema.Name);
}

internal class VenueHireContractEntityConfiguration : IEntityTypeConfiguration<VenueHireContractEntity>
{
    public void Configure(EntityTypeBuilder<VenueHireContractEntity> builder)
        => builder.ToTable("VenueHireContracts", Schema.Name);
}
