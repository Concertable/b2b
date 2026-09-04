namespace Concertable.B2B.Concert.Domain.ValueObjects;

internal sealed record PaymentCommitment(
    string OperationType,
    string ConsumerCorrelation);
