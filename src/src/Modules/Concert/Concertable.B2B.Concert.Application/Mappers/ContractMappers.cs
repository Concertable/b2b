using System.Net.Mime;
using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Mappers;

internal static class ContractMappers
{
    public static ContractDto ToDto(this ContractEntity a) =>
        new(a.Id,
            a.VenueName,
            a.ArtistName,
            a.Period.Start,
            a.Period.End,
            a.ContractType,
            a.PaymentMethod,
            a.TermsText,
            a.PlatformTermsVersion,
            a.ArtistESignature.ToDto(),
            a.VenueESignature.ToDto(),
            a.CreatedAtUtc);

    public static FileDownload ToFileDownload(this ContractEntity a, byte[] content) =>
        new(content, $"booking-agreement-BA-{a.Id}.pdf", MediaTypeNames.Application.Pdf);
}
