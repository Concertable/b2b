using Concertable.B2B.Concert.Domain.Lifecycle;

namespace Concertable.B2B.Concert.Application.Responses;

internal sealed record FinancialOperation(
    Guid OperationId,
    LifecycleState Status,
    string? FailureCode,
    string? FailureMessage);
