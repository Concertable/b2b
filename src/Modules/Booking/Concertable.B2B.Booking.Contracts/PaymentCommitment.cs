namespace Concertable.B2B.Booking.Contracts;

public sealed record PaymentCommitment(
    string OperationType,
    string ConsumerCorrelation);
