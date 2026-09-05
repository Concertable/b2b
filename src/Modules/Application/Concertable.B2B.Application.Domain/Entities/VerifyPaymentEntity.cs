using Concertable.B2B.Application.Domain.ValueObjects;

namespace Concertable.B2B.Application.Domain.Entities;

internal abstract class VerifyPaymentEntity
{
    public int Id { get; private set; }
    public int ApplicationId { get; private set; }

    protected VerifyPaymentEntity() { }

    protected VerifyPaymentEntity(PaymentVerification verification)
    {
        ApplicationId = verification.ApplicationId;
    }

    internal abstract PaymentVerification ToValue();

    internal static VerifyPaymentEntity Create(PaymentVerification verification) => verification switch
    {
        SuccessfulPaymentVerification succeeded => new SucceededVerifyPaymentEntity(succeeded),
        FailedPaymentVerification failed => new FailedVerifyPaymentEntity(failed),
        _ => throw new ArgumentOutOfRangeException(nameof(verification), verification, null)
    };
}

internal sealed class SucceededVerifyPaymentEntity : VerifyPaymentEntity
{
    private SucceededVerifyPaymentEntity() { }

    internal SucceededVerifyPaymentEntity(SuccessfulPaymentVerification verification) : base(verification) { }

    internal override PaymentVerification ToValue() =>
        new SuccessfulPaymentVerification(ApplicationId);
}

internal sealed class FailedVerifyPaymentEntity : VerifyPaymentEntity
{
    public string Code { get; private set; } = null!;
    public string Message { get; private set; } = null!;

    private FailedVerifyPaymentEntity() { }

    internal FailedVerifyPaymentEntity(FailedPaymentVerification verification) : base(verification)
    {
        Code = verification.Failure.Code;
        Message = verification.Failure.Message;
    }

    internal override PaymentVerification ToValue() =>
        new FailedPaymentVerification(
            ApplicationId,
            new PaymentVerificationFailure(Code, Message));
}
