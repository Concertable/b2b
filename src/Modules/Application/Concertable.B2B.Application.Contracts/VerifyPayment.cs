namespace Concertable.B2B.Application.Contracts;

public abstract record VerifyPayment
{
    protected VerifyPayment(int applicationId)
    {
        if (applicationId <= 0)
            throw new ArgumentOutOfRangeException(nameof(applicationId));

        ApplicationId = applicationId;
    }

    public int ApplicationId { get; }
}

public sealed record VerifyPaymentSucceeded : VerifyPayment
{
    public VerifyPaymentSucceeded(int applicationId)
        : base(applicationId) { }
}

public sealed record VerifyPaymentFailed : VerifyPayment
{
    public VerifyPaymentFailed(
        int applicationId,
        VerifyPaymentError error)
        : base(applicationId)
    {
        ArgumentNullException.ThrowIfNull(error);
        Error = error;
    }

    public VerifyPaymentError Error { get; }
}

public sealed record VerifyPaymentError
{
    public VerifyPaymentError(string code, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Code = code;
        Message = message;
    }

    public string Code { get; }
    public string Message { get; }
}
