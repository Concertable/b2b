using System.ComponentModel;
using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Contracts;
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
    public Signature ArtistSignature { get; private set; } = null!;
    public Signature VenueSignature { get; private set; } = null!;
    public string? PdfBlobName { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private ContractEntity() { }

    public static ContractEntity Create(
        int bookingId,
        AcceptedApplication application,
        DateTime createdAtUtc)
    {
        if (bookingId <= 0)
            throw new ArgumentOutOfRangeException(nameof(bookingId));

        return new ContractEntity
        {
            BookingId = bookingId,
            VenueTenantId = application.VenueTenantId,
            ArtistTenantId = application.ArtistTenantId,
            VenueId = application.VenueId,
            VenueName = application.VenueName,
            ArtistId = application.ArtistId,
            ArtistName = application.ArtistName,
            Period = new DateRange(application.StartDate, application.EndDate),
            DealType = application.DealType,
            PaymentMethod = application.PaymentMethod,
            TermsText = application.TermsText,
            PlatformTermsVersion = application.PlatformTermsVersion,
            ArtistSignature = application.ArtistSignature,
            VenueSignature = application.VenueSignature,
            CreatedAtUtc = createdAtUtc,
            PdfBlobName = $"contracts/{bookingId}-{Guid.NewGuid():N}.pdf"
        };
    }
}
