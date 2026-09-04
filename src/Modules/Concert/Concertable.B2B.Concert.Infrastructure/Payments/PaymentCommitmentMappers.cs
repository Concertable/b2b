using Concertable.B2B.Concert.Domain.ValueObjects;
using Concertable.Payment.Contracts;

namespace Concertable.B2B.Concert.Infrastructure.Payments;

internal static class PaymentCommitmentMappers
{
    extension(PaymentCommitment commitment)
    {
        public PaymentOperationReference ToReference() =>
            new(commitment.OperationType, commitment.ConsumerCorrelation);
    }
}
