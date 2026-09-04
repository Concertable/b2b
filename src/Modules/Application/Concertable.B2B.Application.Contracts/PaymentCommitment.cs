namespace Concertable.B2B.Application.Contracts;

public sealed record PaymentCommitment(
    string OperationType,
    string ConsumerCorrelation);
