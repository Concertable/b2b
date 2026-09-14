namespace Concertable.B2B.Application.Domain.ValueObjects;

internal abstract record PaymentVerification(int ApplicationId);

internal sealed record SuccessfulPaymentVerification(int ApplicationId)
    : PaymentVerification(ApplicationId);

internal sealed record FailedPaymentVerification(
    int ApplicationId,
    PaymentVerificationFailure Failure)
    : PaymentVerification(ApplicationId);

internal sealed record PaymentVerificationFailure(string Code, string Message);
