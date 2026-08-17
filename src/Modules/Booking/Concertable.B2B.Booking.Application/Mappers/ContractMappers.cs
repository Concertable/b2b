using System.Net.Mime;
using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Application.DTOs;
using Concertable.B2B.Booking.Domain.Entities;

namespace Concertable.B2B.Booking.Application.Mappers;

internal static class ContractMappers
{
    public static ContractDto ToDto(this ContractEntity contract) =>
        new(
            contract.Id,
            contract.VenueName,
            contract.ArtistName,
            contract.Period.Start,
            contract.Period.End,
            contract.DealType,
            contract.PaymentMethod,
            contract.TermsText,
            contract.PlatformTermsVersion,
            contract.ArtistSignature.ToDto(),
            contract.VenueSignature.ToDto(),
            contract.CreatedAtUtc);

    public static FileDownload ToFileDownload(this ContractEntity contract, byte[] content) =>
        new(content, $"contract-{contract.Id}.pdf", MediaTypeNames.Application.Pdf);

    private static SignatureDto ToDto(this Signature signature) =>
        new(signature.UserId, signature.AtUtc, signature.SignatoryName);
}
