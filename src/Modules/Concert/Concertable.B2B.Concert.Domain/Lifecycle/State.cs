namespace Concertable.B2B.Concert.Domain.Lifecycle;

public enum State
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
