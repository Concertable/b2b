using Concertable.B2B.Deal.Contracts;

namespace Concertable.B2B.Concert.Application.DTOs;

internal sealed record ContractDto(
    int Id,
    string VenueName,
    string ArtistName,
    DateTime EventStart,
    DateTime EventEnd,
    DealType ContractType,
    PaymentMethod PaymentMethod,
    string TermsText,
    string PlatformTermsVersion,
    ESignatureDto ArtistESignature,
    ESignatureDto VenueESignature,
    DateTime CreatedAtUtc);

internal sealed record ESignatureDto(Guid UserId, DateTime AtUtc, string SignatoryName);
