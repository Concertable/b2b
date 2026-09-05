using Concertable.B2B.Booking.Contracts;
using Concertable.Payment.Contracts;

namespace Concertable.B2B.Concert.Application.Models;

internal abstract record SettlementPreparation
{
    internal sealed record Ready(
        Guid OperationId,
        int ConcertId,
        DealType DealType,
        int BookingId,
        PaymentOperationReference Commitment,
        Guid PayerTenantId,
        Guid PayeeTenantId,
        Money Gross) : SettlementPreparation;

    internal sealed record Terminal(SettlementOutcome Outcome) : SettlementPreparation;
}
