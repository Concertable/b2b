namespace Concertable.B2B.Concert.Domain.State;

public enum ConcertState
{
    Draft,
    Posted,
    CancellationPending,
    CancellationFailed,
    AwaitingSettlement,
    SettlementFailed,
    Complete,
    Cancelled
}
