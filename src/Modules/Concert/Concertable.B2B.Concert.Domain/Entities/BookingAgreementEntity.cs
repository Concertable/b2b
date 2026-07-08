using Concertable.B2B.DataAccess.Application;
using Concertable.Kernel;

namespace Concertable.B2B.Concert.Domain.Entities;

/// <summary>
/// The immutable record of the deal both parties agreed to, snapshotted at Accept.
/// Columns are copies, never references to the live contract — opportunity edits must not
/// change what was agreed. Never update a persisted row's terms.
/// </summary>
public sealed class BookingAgreementEntity : IIdEntity, IVenueArtistTenantScoped
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
    public ContractType ContractType { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }

    public decimal? Fee { get; private set; }
    public decimal? HireFee { get; private set; }
    public decimal? Guarantee { get; private set; }
    public decimal? ArtistDoorPercent { get; private set; }

    public string TermsText { get; private set; } = null!;
    public string PlatformTermsVersion { get; private set; } = null!;

    public Guid? ArtistConsentUserId { get; private set; }
    public DateTime? ArtistConsentAtUtc { get; private set; }
    public string? ArtistConsentIp { get; private set; }
    public string? ArtistConsentUserAgent { get; private set; }
    public Guid? VenueConsentUserId { get; private set; }
    public DateTime? VenueConsentAtUtc { get; private set; }
    public string? VenueConsentIp { get; private set; }
    public string? VenueConsentUserAgent { get; private set; }

    public string? PdfBlobName { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private BookingAgreementEntity() { }

    public static BookingAgreementEntity Create(
        int bookingId,
        int venueId,
        string venueName,
        int artistId,
        string artistName,
        DateRange period,
        IContract contract,
        string termsText,
        string platformTermsVersion,
        DateTime createdAtUtc) => new()
        {
            BookingId = bookingId,
            VenueId = venueId,
            VenueName = venueName,
            ArtistId = artistId,
            ArtistName = artistName,
            Period = period,
            ContractType = contract.ContractType,
            PaymentMethod = contract.PaymentMethod,
            Fee = (contract as FlatFeeContract)?.Fee,
            HireFee = (contract as VenueHireContract)?.HireFee,
            Guarantee = (contract as VersusContract)?.Guarantee,
            ArtistDoorPercent = contract switch
            {
                DoorSplitContract doorSplit => doorSplit.ArtistDoorPercent,
                VersusContract versus => versus.ArtistDoorPercent,
                _ => null
            },
            TermsText = termsText,
            PlatformTermsVersion = platformTermsVersion,
            CreatedAtUtc = createdAtUtc
        };
}
