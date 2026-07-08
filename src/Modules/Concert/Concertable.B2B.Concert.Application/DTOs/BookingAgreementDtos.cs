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
    ConsentDto? ArtistConsent,
    ConsentDto VenueConsent,
    DateTime CreatedAtUtc);

internal sealed record ConsentDto(Guid UserId, DateTime AtUtc);

internal sealed record AgreementPdf(byte[] Content, string FileName);
