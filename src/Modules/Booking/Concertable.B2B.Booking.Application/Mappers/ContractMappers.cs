using System.Net.Mime;
using ContractSignature = Concertable.B2B.Application.Contracts.ContractSignature;
using Concertable.B2B.Booking.Application.DTOs;
using Concertable.B2B.Booking.Domain.Entities;

namespace Concertable.B2B.Booking.Application.Mappers;

internal static class ContractMappers
{
    extension(ContractEntity contract)
    {
        public ContractDto ToDto() =>
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

        public FileDownload ToFileDownload(byte[] content) =>
            new(content, $"contract-{contract.Id}.pdf", MediaTypeNames.Application.Pdf);
    }

    extension(ContractSignature signature)
    {
        private SignatureDto ToDto() =>
            new(signature.UserId, signature.AtUtc, signature.SignatoryName);
    }
}
