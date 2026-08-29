namespace Concertable.B2B.Concert.Domain.Lifecycle;

public enum ConcertTrigger
{
    Post,
    BeginCancellation,
    RecordCancellationFailure,
    Cancel,
    BeginSettlement,
    RecordSettlementFailure,
    CompleteSettlement
}
