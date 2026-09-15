namespace Concertable.B2B.Concert.Contracts;

/// <summary>The subject's portable B2B Concert fragment (GDPR arts. 15/20): the RETAINED statutory financial
/// records their tenants are party to — read-only, never mutated by erasure (they survive for the HMRC
/// six-year / contract-limitation windows).</summary>
public sealed record ConcertExport
{
    public IReadOnlyList<InvoiceExport> Invoices { get; init; } = [];
    public IReadOnlyList<SelfBillingAgreementExport> SelfBillingAgreements { get; init; } = [];
}

public sealed record InvoiceExport
{
    public required string InvoiceNumber { get; init; }
    public DateTime TaxPointUtc { get; init; }
    public decimal Net { get; init; }
    public decimal Vat { get; init; }
    public decimal Gross { get; init; }
    public required string DealType { get; init; }
}

public sealed record SelfBillingAgreementExport
{
    public DateTime AcceptedAtUtc { get; init; }
    public DateTime ExpiresAtUtc { get; init; }
    public required string PlatformTermsVersion { get; init; }
}
