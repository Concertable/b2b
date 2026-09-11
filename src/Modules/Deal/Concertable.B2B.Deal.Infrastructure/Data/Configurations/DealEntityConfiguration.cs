using Concertable.B2B.Deal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Deal.Infrastructure.Data.Configurations;

internal sealed class DealEntityConfiguration : IEntityTypeConfiguration<DealEntity>
{
    public void Configure(EntityTypeBuilder<DealEntity> builder)
    {
        builder.ToTable(Schema.Tables.Deals, Schema.Name);
        builder.UseTptMappingStrategy();
    }
}

internal sealed class FlatFeeDealEntityConfiguration : IEntityTypeConfiguration<FlatFeeDealEntity>
{
    public void Configure(EntityTypeBuilder<FlatFeeDealEntity> builder)
        => builder.ToTable(Schema.Tables.FlatFeeDeals, Schema.Name);
}

internal sealed class DoorSplitDealEntityConfiguration : IEntityTypeConfiguration<DoorSplitDealEntity>
{
    public void Configure(EntityTypeBuilder<DoorSplitDealEntity> builder)
        => builder.ToTable(Schema.Tables.DoorSplitDeals, Schema.Name);
}

internal sealed class VersusDealEntityConfiguration : IEntityTypeConfiguration<VersusDealEntity>
{
    public void Configure(EntityTypeBuilder<VersusDealEntity> builder)
        => builder.ToTable(Schema.Tables.VersusDeals, Schema.Name);
}

internal sealed class VenueHireDealEntityConfiguration : IEntityTypeConfiguration<VenueHireDealEntity>
{
    public void Configure(EntityTypeBuilder<VenueHireDealEntity> builder)
        => builder.ToTable(Schema.Tables.VenueHireDeals, Schema.Name);
}
