using Concertable.B2B.Deal.Contracts.Enums;

namespace Concertable.B2B.Concert.Contracts;

/// <summary>The subject's portable B2B Concert fragment (GDPR arts. 15/20): the RETAINED statutory financial
/// records their tenants are party to — read-only, never mutated by erasure (they survive for the HMRC
/// six-year / contract-limitation windows).</summary>
public sealed record SubjectConcertRecordsDto
{
    public IReadOnlyList<SubjectInvoiceDto> Invoices { get; init; } = [];
    public IReadOnlyList<SubjectSelfBillingAgreementDto> SelfBillingAgreements { get; init; } = [];
}

public sealed record SubjectInvoiceDto
{
    public required string InvoiceNumber { get; init; }
    public DateTime TaxPointUtc { get; init; }
    public decimal Net { get; init; }
    public decimal Vat { get; init; }
    public decimal Gross { get; init; }
    public required DealType DealType { get; init; }
}

public sealed record SubjectSelfBillingAgreementDto
{
    public DateTime AcceptedAtUtc { get; init; }
    public DateTime ExpiresAtUtc { get; init; }
    public required string PlatformTermsVersion { get; init; }
}
