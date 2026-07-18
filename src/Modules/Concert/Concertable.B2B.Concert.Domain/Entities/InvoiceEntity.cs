using System.ComponentModel;
using Concertable.B2B.DataAccess.Application;
using Concertable.Contracts;
using Concertable.Kernel;

namespace Concertable.B2B.Concert.Domain.Entities;

/// <summary>
/// A self-billed VAT invoice for a settled booking — the immutable record of the supply, issued by the
/// customer on the supplier's behalf. Every field is a snapshot frozen at settlement (both parties'
/// identities, the VAT decomposition, the tax point); nothing is an FK to live data. Two-party scoped so
/// both the supplier and the customer can read it, and no one else.
/// </summary>
[DisplayName(DisplayNames.Invoice)]
public sealed class InvoiceEntity : IIdEntity, IVenueArtistTenantScoped
{
    public int Id { get; private set; }
    public Guid VenueTenantId { get; set; }
    public Guid ArtistTenantId { get; set; }
    public int BookingId { get; private set; }
    public BookingEntity Booking { get; private set; } = null!;

    /// <summary>The party who made the supply and on whose behalf the invoice is self-billed — the settlement payee.</summary>
    public InvoiceParty Supplier { get; private set; } = null!;

    /// <summary>The party being billed — the settlement's counterparty (the ticket payee, i.e. the inverse of the supplier).</summary>
    public InvoiceParty Customer { get; private set; } = null!;

    public VatBreakdown Amounts { get; private set; } = null!;

    /// <summary>The gap-free per-supplier sequence value; <see cref="InvoiceNumber"/> is its formatted, human-facing form.</summary>
    public long SequenceNumber { get; private set; }
    public string InvoiceNumber { get; private set; } = null!;

    /// <summary>The tax point — the performance/finish date, not the (possibly-later, async) payment-settlement date.</summary>
    public DateTime TaxPointUtc { get; private set; }
    public DealType DealType { get; private set; }
    public string? PdfBlobName { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private InvoiceEntity() { }

    public static InvoiceEntity Create(
        int bookingId,
        InvoiceParty supplier,
        InvoiceParty customer,
        VatBreakdown amounts,
        long sequenceNumber,
        string invoiceNumber,
        DateTime taxPointUtc,
        DealType dealType,
        DateTime createdAtUtc) => new()
        {
            BookingId = bookingId,
            Supplier = supplier,
            Customer = customer,
            Amounts = amounts,
            SequenceNumber = sequenceNumber,
            InvoiceNumber = invoiceNumber,
            TaxPointUtc = taxPointUtc,
            DealType = dealType,
            CreatedAtUtc = createdAtUtc,
            PdfBlobName = $"invoices/{bookingId}-{Guid.NewGuid():N}.pdf"
        };
}
