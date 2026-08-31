using System.Net;
using Concertable.B2B.Application.Application.Requests;
using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Application.Domain.ValueObjects;

namespace Concertable.B2B.Application.Application.Mappers;

internal static class ApplicationValueMappers
{
    extension(Signature signature)
    {
        public SignatureDto ToDto() =>
            new(
                signature.UserId,
                signature.AtUtc,
                signature.Ip,
                signature.UserAgent,
                signature.SignatoryName,
                signature.DrawnSignatureImage);
    }

    extension(ESignatureRequest eSignature)
    {
        public Signature ToSignature(Guid userId, DateTime atUtc, IPAddress ip, string? userAgent) =>
            new(userId, atUtc, ip, userAgent, eSignature.SignatoryName, eSignature.DrawnSignatureImage);
    }

    extension(PaymentVerification verification)
    {
        public VerifyPayment ToVerifyPayment() => verification switch
        {
            SuccessfulPaymentVerification succeeded =>
                new VerifyPaymentSucceeded(succeeded.ApplicationId, succeeded.ProviderTransactionId),
            FailedPaymentVerification failed =>
                new VerifyPaymentFailed(
                    failed.ApplicationId,
                    failed.ProviderTransactionId,
                    new VerifyPaymentError(failed.Failure.Code, failed.Failure.Message)),
            _ => throw new ArgumentOutOfRangeException(nameof(verification), verification, null)
        };
    }

    extension(VerifyPayment payment)
    {
        public PaymentVerification ToPaymentVerification() => payment switch
        {
            VerifyPaymentSucceeded succeeded =>
                new SuccessfulPaymentVerification(succeeded.ApplicationId, succeeded.ProviderTransactionId),
            VerifyPaymentFailed failed =>
                new FailedPaymentVerification(
                    failed.ApplicationId,
                    failed.ProviderTransactionId,
                    new PaymentVerificationFailure(failed.Error.Code, failed.Error.Message)),
            _ => throw new ArgumentOutOfRangeException(nameof(payment), payment, null)
        };
    }
}
