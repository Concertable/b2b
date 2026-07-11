using Concertable.B2B.Contract.Contracts;

namespace Concertable.B2B.Concert.Application.DTOs;

internal sealed record BookingAgreementDto(
    int Id,
    string VenueName,
    string ArtistName,
    DateTime EventStart,
    DateTime EventEnd,
    ContractType ContractType,
    PaymentMethod PaymentMethod,
    string TermsText,
    string PlatformTermsVersion,
    ESignatureDto? ArtistESignature,
    ESignatureDto VenueESignature,
    DateTime CreatedAtUtc);

internal sealed record ESignatureDto(Guid UserId, DateTime AtUtc, string SignatoryName);

internal sealed record AgreementPdf(byte[] Content, string FileName);
