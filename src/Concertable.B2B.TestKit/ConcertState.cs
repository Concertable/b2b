namespace Concertable.B2B.TestKit;

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
