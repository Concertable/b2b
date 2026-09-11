using Concertable.B2B.Deal.Contracts;

namespace Concertable.B2B.Concert.Application.DTOs;

internal sealed record InvoiceDto(
    int Id,
    string InvoiceNumber,
    DateTime TaxPointUtc,
    DealType DealType,
    InvoicePartyDto Supplier,
    InvoicePartyDto Customer,
    decimal Net,
    decimal Vat,
    decimal Gross,
    decimal VatRate,
    DateTime CreatedAtUtc);

internal sealed record InvoicePartyDto(
    string LegalName,
    string? VatNumber,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string Postcode,
    string Country);
