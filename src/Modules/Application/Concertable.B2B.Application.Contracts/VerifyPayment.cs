using Concertable.Kernel;

namespace Concertable.B2B.Application.Contracts;

public abstract record VerifyPayment : IDomainEvent
{
    protected VerifyPayment(int applicationId, string providerTransactionId)
    {
        if (applicationId <= 0)
            throw new ArgumentOutOfRangeException(nameof(applicationId));
        ArgumentException.ThrowIfNullOrWhiteSpace(providerTransactionId);

        this.ApplicationId = applicationId;
        this.ProviderTransactionId = providerTransactionId;
    }

    public int ApplicationId { get; }
    public string ProviderTransactionId { get; }
}

public sealed record VerifyPaymentSucceeded : VerifyPayment
{
    public VerifyPaymentSucceeded(int applicationId, string providerTransactionId)
        : base(applicationId, providerTransactionId) { }
}

public sealed record VerifyPaymentFailed : VerifyPayment
{
    public VerifyPaymentFailed(
        int applicationId,
        string providerTransactionId,
        VerifyPaymentError error)
        : base(applicationId, providerTransactionId)
    {
        ArgumentNullException.ThrowIfNull(error);
        this.Error = error;
    }

    public VerifyPaymentError Error { get; }
}

public sealed record VerifyPaymentError
{
    public VerifyPaymentError(string code, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        this.Code = code;
        this.Message = message;
    }

    public string Code { get; }
    public string Message { get; }
}
