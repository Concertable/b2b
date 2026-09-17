using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Infrastructure.Mappers;

internal static class SubjectRecordMappers
{
    extension(InvoiceEntity invoice)
    {
        public SubjectInvoiceDto ToSubjectInvoiceDto() => new()
        {
            InvoiceNumber = invoice.InvoiceNumber,
            TaxPointUtc = invoice.TaxPointUtc,
            Net = invoice.Amounts.Net,
            Vat = invoice.Amounts.Vat,
            Gross = invoice.Amounts.Gross,
            DealType = invoice.DealType,
        };
    }

    extension(SelfBillingAgreementEntity agreement)
    {
        public SubjectSelfBillingAgreementDto ToSubjectSelfBillingAgreementDto() => new()
        {
            AcceptedAtUtc = agreement.AcceptedAtUtc,
            ExpiresAtUtc = agreement.ExpiresAtUtc,
            PlatformTermsVersion = agreement.PlatformTermsVersion,
        };
    }
}
