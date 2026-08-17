using System.Net;
using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Concertable.B2B.Booking.Infrastructure.Data.Configurations;

internal sealed class ContractEntityConfiguration : IEntityTypeConfiguration<ContractEntity>
{
    private static readonly ValueConverter<IPAddress, string> IpConverter =
        new(ip => ip.ToString(), text => IPAddress.Parse(text));

    public void Configure(EntityTypeBuilder<ContractEntity> builder)
    {
        builder.ToTable(Schema.Tables.Contracts, Schema.Name);
        builder.HasOne<BookingEntity>()
            .WithOne()
            .HasForeignKey<ContractEntity>(contract => contract.BookingId)
            .IsRequired()
            .OnDelete(DeleteBehavior.NoAction);
        builder.ComplexProperty(contract => contract.Period, period =>
        {
            period.Property(value => value.Start).HasColumnName("Period_Start");
            period.Property(value => value.End).HasColumnName("Period_End");
        });
        builder.ComplexProperty(contract => contract.ArtistSignature, ConfigureSignature);
        builder.ComplexProperty(contract => contract.VenueSignature, ConfigureSignature);
    }

    private static void ConfigureSignature(ComplexPropertyBuilder<Signature> builder)
    {
        builder.Property(signature => signature.Ip).HasConversion(IpConverter).HasMaxLength(45);
        builder.Property(signature => signature.UserAgent).HasMaxLength(512);
    }
}
