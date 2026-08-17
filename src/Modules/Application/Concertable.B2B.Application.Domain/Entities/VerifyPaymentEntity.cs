using Concertable.B2B.Application.Contracts;

namespace Concertable.B2B.Application.Domain.Entities;

internal abstract class VerifyPaymentEntity
{
    public int Id { get; private set; }
    public int ApplicationId { get; private set; }
    public string ProviderTransactionId { get; private set; } = null!;

    protected VerifyPaymentEntity() { }

    protected VerifyPaymentEntity(VerifyPayment payment)
    {
        this.ApplicationId = payment.ApplicationId;
        this.ProviderTransactionId = payment.ProviderTransactionId;
    }

    internal abstract VerifyPayment ToContract();

    internal static VerifyPaymentEntity Create(VerifyPayment payment) => payment switch
    {
        VerifyPaymentSucceeded succeeded => new SucceededVerifyPaymentEntity(succeeded),
        VerifyPaymentFailed failed => new FailedVerifyPaymentEntity(failed),
        _ => throw new ArgumentOutOfRangeException(nameof(payment), payment, null)
    };
}

internal sealed class SucceededVerifyPaymentEntity : VerifyPaymentEntity
{
    private SucceededVerifyPaymentEntity() { }

    internal SucceededVerifyPaymentEntity(VerifyPaymentSucceeded payment) : base(payment) { }

    internal override VerifyPayment ToContract() =>
        new VerifyPaymentSucceeded(ApplicationId, ProviderTransactionId);
}

internal sealed class FailedVerifyPaymentEntity : VerifyPaymentEntity
{
    public string Code { get; private set; } = null!;
    public string Message { get; private set; } = null!;

    private FailedVerifyPaymentEntity() { }

    internal FailedVerifyPaymentEntity(VerifyPaymentFailed payment) : base(payment)
    {
        this.Code = payment.Error.Code;
        this.Message = payment.Error.Message;
    }

    internal override VerifyPayment ToContract() =>
        new VerifyPaymentFailed(
            ApplicationId,
            ProviderTransactionId,
            new VerifyPaymentError(Code, Message));
}
