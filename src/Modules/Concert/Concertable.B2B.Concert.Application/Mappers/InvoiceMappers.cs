using System.Net.Mime;
using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Mappers;

internal static class InvoiceMappers
{
    public static FileDownload ToFileDownload(this InvoiceEntity i, byte[] content) =>
        new(content, $"{i.InvoiceNumber}.pdf", MediaTypeNames.Application.Pdf);

    public static InvoiceDto ToDto(this InvoiceEntity i) =>
        new(i.Id,
            i.InvoiceNumber,
            i.TaxPointUtc,
            i.DealType,
            i.Supplier.ToDto(),
            i.Customer.ToDto(),
            i.Amounts.Net,
            i.Amounts.Vat,
            i.Amounts.Gross,
            i.Amounts.Rate,
            i.CreatedAtUtc);

    private static InvoicePartyDto ToDto(this InvoiceParty p) =>
        new(p.LegalName,
            p.VatNumber,
            p.AddressLine1,
            p.AddressLine2,
            p.City,
            p.Postcode,
            p.Country);
}
