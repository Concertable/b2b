using Concertable.B2B.Booking.Domain.State;

namespace Concertable.B2B.Booking.Application.Models;

internal abstract record FinancialOperationEvidence
{
    protected FinancialOperationEvidence(
        int applicationId,
        FinancialOperation operation,
        string providerReferenceId)
    {
        if (applicationId <= 0)
            throw new ArgumentOutOfRangeException(nameof(applicationId));
        ArgumentException.ThrowIfNullOrWhiteSpace(providerReferenceId);

        this.ApplicationId = applicationId;
        this.Operation = operation;
        this.ProviderReferenceId = providerReferenceId;
    }

    public int ApplicationId { get; }
    public FinancialOperation Operation { get; }
    public string ProviderReferenceId { get; }
}

internal sealed record FinancialOperationSucceeded : FinancialOperationEvidence
{
    public FinancialOperationSucceeded(
        int applicationId,
        FinancialOperation operation,
        string providerReferenceId)
        : base(applicationId, operation, providerReferenceId) { }
}

internal sealed record FinancialOperationFailed : FinancialOperationEvidence
{
    public FinancialOperationFailed(
        int applicationId,
        FinancialOperation operation,
        string providerReferenceId,
        FinancialOperationError error)
        : base(applicationId, operation, providerReferenceId)
    {
        ArgumentNullException.ThrowIfNull(error);
        this.Error = error;
    }

    public FinancialOperationError Error { get; }
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
