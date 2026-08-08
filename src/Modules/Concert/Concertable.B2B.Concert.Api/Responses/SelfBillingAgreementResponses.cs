namespace Concertable.B2B.Concert.Api.Responses;

internal enum SelfBillingAgreementStatus
{
    None,
    Active,
    Expired,
}

internal sealed record SelfBillingAgreementResponse
{
    public SelfBillingAgreementStatus Status { get; init; }
    public string? SupplierLegalName { get; init; }
    public DateTime? AcceptedAtUtc { get; init; }
    public DateTime? ExpiresAtUtc { get; init; }
    public required SelfBillingAgreementActions Actions { get; init; }
}

internal sealed record SelfBillingAgreementActions(ActionLink? Grant, ActionLink? Renew, ActionLink? Pdf);
