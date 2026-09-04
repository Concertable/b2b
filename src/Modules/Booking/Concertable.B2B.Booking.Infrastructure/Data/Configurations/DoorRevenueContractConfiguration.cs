using Concertable.B2B.Booking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Booking.Infrastructure.Data.Configurations;

internal sealed class DoorRevenueContractConfiguration : IEntityTypeConfiguration<DoorRevenueContract>
{
    public void Configure(EntityTypeBuilder<DoorRevenueContract> builder) =>
        builder.Property(contract => contract.ArtistDoorPercent)
            .HasColumnName(nameof(DoorRevenueContract.ArtistDoorPercent));
}
