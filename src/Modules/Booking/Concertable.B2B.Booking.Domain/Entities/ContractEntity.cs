using System.ComponentModel;
using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Booking.Domain.ValueObjects;
using Concertable.B2B.DataAccess.Application;
using Concertable.B2B.Deal.Contracts.Enums;
using Concertable.Kernel;
using Concertable.Kernel.ValueObjects;

namespace Concertable.B2B.Booking.Domain.Entities;

[DisplayName(Booking.Contracts.DisplayNames.Contract)]
public sealed class ContractEntity : IIdEntity, IVenueArtistTenantScoped
{
    public int Id { get; private set; }
    public Guid VenueTenantId { get; private set; }
    public Guid ArtistTenantId { get; private set; }
    public int BookingId { get; private set; }
    public int VenueId { get; private set; }
    public string VenueName { get; private set; } = null!;
    public int ArtistId { get; private set; }
    public string ArtistName { get; private set; } = null!;
    public DateRange Period { get; private set; } = null!;
    public DealType DealType { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public string TermsText { get; private set; } = null!;
    public string PlatformTermsVersion { get; private set; } = null!;
    internal Signature ArtistSignature { get; private set; } = null!;
    internal Signature VenueSignature { get; private set; } = null!;
    public string? PdfBlobName { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private ContractEntity() { }

    internal static ContractEntity Create(
        int bookingId,
        BookingAcceptance acceptance,
        DateTime createdAtUtc)
    {
        if (bookingId <= 0)
            throw new ArgumentOutOfRangeException(nameof(bookingId));

        return new ContractEntity
        {
            BookingId = bookingId,
            VenueTenantId = acceptance.VenueTenantId,
            ArtistTenantId = acceptance.ArtistTenantId,
            VenueId = acceptance.VenueId,
            VenueName = acceptance.VenueName,
            ArtistId = acceptance.ArtistId,
            ArtistName = acceptance.ArtistName,
            Period = new DateRange(acceptance.StartDate, acceptance.EndDate),
            DealType = acceptance.DealType,
            PaymentMethod = acceptance.PaymentMethod,
            TermsText = acceptance.TermsText,
            PlatformTermsVersion = acceptance.PlatformTermsVersion,
            ArtistSignature = acceptance.ArtistSignature,
            VenueSignature = acceptance.VenueSignature,
            CreatedAtUtc = createdAtUtc,
            PdfBlobName = $"contracts/{bookingId}-{Guid.NewGuid():N}.pdf"
        };
    }
}
