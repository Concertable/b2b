using Concertable.B2B.Booking.Contracts;
using Concertable.Payment.Contracts;

namespace Concertable.B2B.Booking.Infrastructure.Payments;

internal static class PaymentCommitmentMappers
{
    extension(PaymentCommitment commitment)
    {
        public PaymentOperationReference ToReference() =>
            new(commitment.OperationType, commitment.ConsumerCorrelation);
    }
}
