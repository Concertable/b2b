using System.Net.Mime;
using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Mappers;

internal static class SelfBillingAgreementMappers
{
    public static SelfBillingAgreementDto ToDto(this SelfBillingAgreementEntity a) =>
        new(a.Id,
            a.Supplier.LegalName,
            a.Supplier.VatNumber,
            a.AcceptedAtUtc,
            a.ExpiresAtUtc,
            a.PlatformTermsVersion,
            a.CreatedAtUtc);

    public static FileDownload ToFileDownload(this SelfBillingAgreementEntity a, byte[] content) =>
        new(content, $"self-billing-agreement-{a.Id}.pdf", MediaTypeNames.Application.Pdf);
}
