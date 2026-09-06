using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Application.Domain.ValueObjects;

namespace Concertable.B2B.Application.Application.Mappers;

internal static class PaymentMappers
{
    extension(PaymentVerification verification)
    {
        public VerifyPayment ToVerifyPayment() => verification switch
        {
            SuccessfulPaymentVerification succeeded =>
                new VerifyPaymentSucceeded(succeeded.ApplicationId),
            FailedPaymentVerification failed =>
                new VerifyPaymentFailed(
                    failed.ApplicationId,
                    new VerifyPaymentError(failed.Failure.Code, failed.Failure.Message)),
            _ => throw new ArgumentOutOfRangeException(nameof(verification), verification, null)
        };
    }

    extension(VerifyPayment payment)
    {
        public PaymentVerification ToPaymentVerification() => payment switch
        {
            VerifyPaymentSucceeded succeeded =>
                new SuccessfulPaymentVerification(succeeded.ApplicationId),
            VerifyPaymentFailed failed =>
                new FailedPaymentVerification(
                    failed.ApplicationId,
                    new PaymentVerificationFailure(failed.Error.Code, failed.Error.Message)),
            _ => throw new ArgumentOutOfRangeException(nameof(payment), payment, null)
        };
    }
}
