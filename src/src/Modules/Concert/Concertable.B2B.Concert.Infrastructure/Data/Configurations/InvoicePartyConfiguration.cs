using Concertable.B2B.Concert.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Concert.Infrastructure.Data.Configurations;

/// <summary>
/// Shared mapping for the <see cref="InvoiceParty"/> value object — one definition for both the supplier
/// and the customer snapshot. A complex type (stored inline in the invoice row, copied by value), so the
/// same helper configures both owners; EF prefixes the columns with the owning property name
/// (<c>Supplier_*</c> / <c>Customer_*</c>), keeping them distinct.
/// </summary>
internal static class InvoicePartyConfiguration
{
    public static void Configure(ComplexPropertyBuilder<InvoiceParty> builder)
    {
        builder.Property(p => p.LegalName).HasMaxLength(512);
        builder.Property(p => p.VatNumber).HasMaxLength(32);
        builder.Property(p => p.AddressLine1).HasMaxLength(256);
        builder.Property(p => p.AddressLine2).HasMaxLength(256);
        builder.Property(p => p.City).HasMaxLength(128);
        builder.Property(p => p.Postcode).HasMaxLength(32);
        builder.Property(p => p.Country).HasMaxLength(128);
    }
}
