using Concertable.B2B.Concert.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Concert.Infrastructure.Data.Configurations;

internal sealed class ContractEntityConfiguration : IEntityTypeConfiguration<ContractEntity>
{
    public void Configure(EntityTypeBuilder<ContractEntity> builder)
    {
        builder.ToTable(Schema.Tables.Contracts, Schema.Name);
        builder.HasOne(a => a.Booking)
            .WithOne()
            .HasForeignKey<ContractEntity>(a => a.BookingId)
            .IsRequired()
            .OnDelete(DeleteBehavior.NoAction);
        builder.OwnsOne(a => a.Period, p =>
        {
            p.Property(x => x.Start).HasColumnName("Period_Start");
            p.Property(x => x.End).HasColumnName("Period_End");
        });
        builder.ComplexProperty(a => a.ArtistESignature, ESignatureConfiguration.Configure);
        builder.ComplexProperty(a => a.VenueESignature, ESignatureConfiguration.Configure);
    }
}
