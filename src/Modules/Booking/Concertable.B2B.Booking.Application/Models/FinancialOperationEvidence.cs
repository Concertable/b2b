using Concertable.B2B.Booking.Domain.State;

namespace Concertable.B2B.Booking.Application.Models;

internal abstract record FinancialOperationEvidence
{
    protected FinancialOperationEvidence(FinancialOperation operation)
    {
        this.Operation = operation;
    }

    public FinancialOperation Operation { get; }
}

internal abstract record FinancialOperationSucceeded : FinancialOperationEvidence
{
    protected FinancialOperationSucceeded(
        FinancialOperation operation,
        string providerReferenceId)
        : base(operation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerReferenceId);
        this.ProviderReferenceId = providerReferenceId;
    }

    public string ProviderReferenceId { get; }
}

internal sealed record VerifyPaymentSucceededEvidence : FinancialOperationSucceeded
{
    public VerifyPaymentSucceededEvidence(int applicationId, string providerReferenceId)
        : base(FinancialOperation.VerifyPayment, providerReferenceId)
    {
        if (applicationId <= 0)
            throw new ArgumentOutOfRangeException(nameof(applicationId));
        this.ApplicationId = applicationId;
    }

    public int ApplicationId { get; }
}

internal sealed record AcceptanceFinancialOperationSucceeded : FinancialOperationSucceeded
{
    public AcceptanceFinancialOperationSucceeded(
        Guid operationId,
        int bookingId,
        FinancialOperation operation,
        string providerReferenceId)
        : base(operation, providerReferenceId)
    {
        if (operationId == Guid.Empty)
            throw new ArgumentException("An acceptance operation ID is required.", nameof(operationId));
        if (bookingId <= 0)
            throw new ArgumentOutOfRangeException(nameof(bookingId));
        if (operation == FinancialOperation.VerifyPayment)
            throw new ArgumentOutOfRangeException(nameof(operation), operation, null);

        this.OperationId = operationId;
        this.BookingId = bookingId;
    }

    public Guid OperationId { get; }
    public int BookingId { get; }
}

internal abstract record FinancialOperationFailed : FinancialOperationEvidence
{
    protected FinancialOperationFailed(
        FinancialOperation operation,
        FinancialOperationError error)
        : base(operation)
    {
        ArgumentNullException.ThrowIfNull(error);
        this.Error = error;
    }

    public FinancialOperationError Error { get; }
}

internal sealed record VerifyPaymentFailedEvidence : FinancialOperationFailed
{
    public VerifyPaymentFailedEvidence(
        int applicationId,
        string providerReferenceId,
        FinancialOperationError error)
        : base(FinancialOperation.VerifyPayment, error)
    {
        if (applicationId <= 0)
            throw new ArgumentOutOfRangeException(nameof(applicationId));
        ArgumentException.ThrowIfNullOrWhiteSpace(providerReferenceId);

        this.ApplicationId = applicationId;
        this.ProviderReferenceId = providerReferenceId;
    }

    public int ApplicationId { get; }
    public string ProviderReferenceId { get; }
}

internal sealed record AcceptanceFinancialOperationRejected : FinancialOperationFailed
{
    public AcceptanceFinancialOperationRejected(
        Guid operationId,
        int bookingId,
        FinancialOperation operation,
        FinancialOperationError error)
        : base(operation, error)
    {
        if (operationId == Guid.Empty)
            throw new ArgumentException("An acceptance operation ID is required.", nameof(operationId));
        if (bookingId <= 0)
            throw new ArgumentOutOfRangeException(nameof(bookingId));
        if (operation == FinancialOperation.VerifyPayment)
            throw new ArgumentOutOfRangeException(nameof(operation), operation, null);

        this.OperationId = operationId;
        this.BookingId = bookingId;
    }

    public Guid OperationId { get; }
    public int BookingId { get; }
}

internal sealed record FinancialOperationError
{
    public FinancialOperationError(string code, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        this.Code = code;
        this.Message = message;
    }

    public string Code { get; }
    public string Message { get; }
}
