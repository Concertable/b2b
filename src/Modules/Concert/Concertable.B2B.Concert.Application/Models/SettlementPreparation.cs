using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Concert.Domain.ValueObjects;
using PaymentCommitment = Concertable.B2B.Concert.Domain.ValueObjects.PaymentCommitment;

namespace Concertable.B2B.Concert.Application.Models;

internal abstract record SettlementPreparation
{
    internal sealed record Ready(
        Guid OperationId,
        int ConcertId,
        DealType DealType,
        int BookingId,
        PaymentCommitment Commitment,
        Guid PayerTenantId,
        Guid PayeeTenantId,
        Money Gross) : SettlementPreparation;

    internal sealed record Terminal(SettlementOutcome Outcome) : SettlementPreparation;
}
