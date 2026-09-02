using Concertable.B2B.Booking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Booking.Infrastructure.Data.Configurations;

internal sealed class VenueHireContractConfiguration : IEntityTypeConfiguration<VenueHireContract>
{
    public void Configure(EntityTypeBuilder<VenueHireContract> builder) =>
        builder.Property(contract => contract.PaymentMethodId)
            .HasColumnName(ContractColumns.PaymentMethodId)
            .HasMaxLength(255);
}

internal sealed class DoorSplitContractConfiguration : IEntityTypeConfiguration<DoorSplitContract>
{
    public void Configure(EntityTypeBuilder<DoorSplitContract> builder)
    {
        builder.Property(contract => contract.PaymentMethodId)
            .HasColumnName(ContractColumns.PaymentMethodId)
            .HasMaxLength(255);
        builder.Property(contract => contract.ArtistDoorPercent)
            .HasColumnName(ContractColumns.ArtistDoorPercent);
    }
}

internal sealed class VersusContractConfiguration : IEntityTypeConfiguration<VersusContract>
{
    public void Configure(EntityTypeBuilder<VersusContract> builder)
    {
        builder.Property(contract => contract.PaymentMethodId)
            .HasColumnName(ContractColumns.PaymentMethodId)
            .HasMaxLength(255);
        builder.Property(contract => contract.ArtistDoorPercent)
            .HasColumnName(ContractColumns.ArtistDoorPercent);
    }
}

internal static class ContractColumns
{
    public const string PaymentMethodId = nameof(PaymentMethodId);
    public const string ArtistDoorPercent = nameof(ArtistDoorPercent);
}
