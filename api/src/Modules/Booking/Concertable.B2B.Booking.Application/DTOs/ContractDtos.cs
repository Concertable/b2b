using Concertable.B2B.Deal.Contracts.Enums;

namespace Concertable.B2B.Booking.Application.DTOs;

internal sealed record ContractDto(
    int Id,
    string VenueName,
    string ArtistName,
    DateTime EventStart,
    DateTime EventEnd,
    DealType DealType,
    PaymentMethod PaymentMethod,
    string TermsText,
    string PlatformTermsVersion,
    SignatureDto ArtistSignature,
    SignatureDto VenueSignature,
    DateTime CreatedAtUtc);

internal sealed record SignatureDto(Guid UserId, DateTime AtUtc, string SignatoryName);
