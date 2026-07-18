namespace Concertable.B2B.Concert.Domain.Entities;

/// <summary>
/// A snapshot of one party's legal identity on a self-billed invoice, frozen at settlement. Copied by
/// value from the tenant's tax-compliance record so a later edit to the tenant never rewrites an issued
/// invoice. <see cref="VatNumber"/> is absent when that party is not VAT-registered.
/// </summary>
public sealed record InvoiceParty(
    Guid TenantId,
    string LegalName,
    string? VatNumber,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string Postcode,
    string Country);
