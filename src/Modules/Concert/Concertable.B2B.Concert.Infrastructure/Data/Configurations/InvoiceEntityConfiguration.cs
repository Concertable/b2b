using Concertable.B2B.Concert.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Concert.Infrastructure.Data.Configurations;

internal sealed class InvoiceEntityConfiguration : IEntityTypeConfiguration<InvoiceEntity>
{
    public void Configure(EntityTypeBuilder<InvoiceEntity> builder)
    {
        builder.ToTable(Schema.Tables.Invoices, Schema.Name);

        builder.HasOne(i => i.Booking)
            .WithOne()
            .HasForeignKey<InvoiceEntity>(i => i.BookingId)
            .IsRequired()
            .OnDelete(DeleteBehavior.NoAction);

        builder.Property(i => i.InvoiceNumber).HasMaxLength(64);

        builder.ComplexProperty(i => i.Amounts, a =>
        {
            a.Property(x => x.Net).HasPrecision(18, 2);
            a.Property(x => x.Vat).HasPrecision(18, 2);
            a.Property(x => x.Gross).HasPrecision(18, 2);
            a.Property(x => x.Rate).HasPrecision(5, 4);
        });

        builder.ComplexProperty(i => i.Supplier, InvoicePartyConfiguration.Configure);
        builder.ComplexProperty(i => i.Customer, InvoicePartyConfiguration.Configure);
    }
}
