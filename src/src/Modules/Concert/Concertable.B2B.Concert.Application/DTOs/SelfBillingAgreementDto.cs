namespace Concertable.B2B.Concert.Application.DTOs;

internal sealed record SelfBillingAgreementDto(
    int Id,
    string SupplierLegalName,
    string? SupplierVatNumber,
    DateTime AcceptedAtUtc,
    DateTime ExpiresAtUtc,
    string PlatformTermsVersion,
    DateTime CreatedAtUtc);

internal sealed record SelfBillingAgreementStatusDto(
    SelfBillingAgreementDto? Agreement,
    bool IsInForce,
    bool CanRenew);
