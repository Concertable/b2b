using Concertable.B2B.Concert.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Concert.Infrastructure.Data.Configurations;

internal sealed class BookingAgreementEntityConfiguration : IEntityTypeConfiguration<BookingAgreementEntity>
{
    public void Configure(EntityTypeBuilder<BookingAgreementEntity> builder)
    {
        builder.ToTable(Schema.Tables.BookingAgreements, Schema.Name);
        builder.HasOne(a => a.Booking)
            .WithOne()
            .HasForeignKey<BookingAgreementEntity>(a => a.BookingId)
            .IsRequired()
            .OnDelete(DeleteBehavior.NoAction);
        builder.OwnsOne(a => a.Period, p =>
        {
            p.Property(x => x.Start).HasColumnName("Period_Start");
            p.Property(x => x.End).HasColumnName("Period_End");
        });
    }
}
