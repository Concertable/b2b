using Concertable.B2B.Deal.Contracts.Enums;

namespace Concertable.B2B.Concert.Contracts;

public sealed record SubjectConcertRecordsDto
{
    public IReadOnlyList<SubjectInvoiceDto> Invoices { get; init; } = [];
    public IReadOnlyList<SubjectSelfBillingAgreementDto> SelfBillingAgreements { get; init; } = [];
}

public sealed record SubjectInvoiceDto
{
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
