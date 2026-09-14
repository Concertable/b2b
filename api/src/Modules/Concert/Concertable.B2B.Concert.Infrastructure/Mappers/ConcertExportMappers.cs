using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Infrastructure.Mappers;

internal static class ConcertExportMappers
{
    extension(InvoiceEntity invoice)
    {
        public InvoiceExport ToInvoiceExport() => new()
        {
            InvoiceNumber = invoice.InvoiceNumber,
            TaxPointUtc = invoice.TaxPointUtc,
            Net = invoice.Amounts.Net,
            Vat = invoice.Amounts.Vat,
            Gross = invoice.Amounts.Gross,
            DealType = invoice.DealType.ToString(),
        };
    }

    extension(SelfBillingAgreementEntity agreement)
    {
        public SelfBillingAgreementExport ToSelfBillingAgreementExport() => new()
        {
            AcceptedAtUtc = agreement.AcceptedAtUtc,
            ExpiresAtUtc = agreement.ExpiresAtUtc,
            PlatformTermsVersion = agreement.PlatformTermsVersion,
        };
    }
}
