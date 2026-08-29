namespace Concertable.B2B.Concert.Domain.Lifecycle;

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
