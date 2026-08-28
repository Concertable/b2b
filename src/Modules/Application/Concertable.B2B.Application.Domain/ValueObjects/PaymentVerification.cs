namespace Concertable.B2B.Application.Domain.ValueObjects;

internal abstract record PaymentVerification(int ApplicationId, string ProviderTransactionId);

internal sealed record SuccessfulPaymentVerification(
    int ApplicationId,
    string ProviderTransactionId)
    : PaymentVerification(ApplicationId, ProviderTransactionId);

internal sealed record FailedPaymentVerification(
    int ApplicationId,
    string ProviderTransactionId,
    PaymentVerificationFailure Failure)
    : PaymentVerification(ApplicationId, ProviderTransactionId);

internal sealed record PaymentVerificationFailure(string Code, string Message);
