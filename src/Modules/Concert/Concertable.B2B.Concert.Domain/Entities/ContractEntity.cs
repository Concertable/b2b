using System.ComponentModel;
using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.DataAccess.Application;
using Concertable.Kernel;

namespace Concertable.B2B.Concert.Domain.Entities;

/// <summary>
/// A by-value snapshot of the deal's terms, copied at creation and immutable after (private setters),
/// so a later edit to the live deal can't change what was signed. Created by <c>ContractIssuer</c>
/// during Accept — but that timing is convention, not an invariant the model enforces (see TECH_DEBT).
/// </summary>
[DisplayName(DisplayNames.Contract)]
public sealed class ContractEntity : IIdEntity, IVenueArtistTenantScoped
{
    public int Id { get; private set; }
    public Guid VenueTenantId { get; set; }
    public Guid ArtistTenantId { get; set; }
    public int BookingId { get; private set; }
    public BookingEntity Booking { get; private set; } = null!;

    public int VenueId { get; private set; }
    public string VenueName { get; private set; } = null!;
    public int ArtistId { get; private set; }
    public string ArtistName { get; private set; } = null!;

    public DateRange Period { get; private set; } = null!;
    public DealType DealType { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }

    public string TermsText { get; private set; } = null!;
    public string PlatformTermsVersion { get; private set; } = null!;

    public ESignature ArtistESignature { get; private set; } = null!;
    public ESignature VenueESignature { get; private set; } = null!;

    public string? PdfBlobName { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private ContractEntity() { }

    public static ContractEntity Create(
        int bookingId,
        int venueId,
        string venueName,
        int artistId,
        string artistName,
        DateRange period,
        IDeal deal,
        string termsText,
        string platformTermsVersion,
        ESignature artistESignature,
        ESignature venueESignature,
        DateTime createdAtUtc) => new()
        {
            BookingId = bookingId,
            VenueId = venueId,
            VenueName = venueName,
            ArtistId = artistId,
            ArtistName = artistName,
            Period = period,
            DealType = deal.DealType,
            PaymentMethod = deal.PaymentMethod,
            TermsText = termsText,
            PlatformTermsVersion = platformTermsVersion,
            ArtistESignature = artistESignature,
            VenueESignature = venueESignature,
            CreatedAtUtc = createdAtUtc,
            PdfBlobName = $"contracts/{bookingId}-{Guid.NewGuid():N}.pdf"
        };
}
