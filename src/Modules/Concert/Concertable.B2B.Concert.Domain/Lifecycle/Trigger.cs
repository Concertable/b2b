namespace Concertable.B2B.Concert.Domain.Lifecycle;

public enum Trigger
{
    Post,
    BeginCancellation,
    RecordCancellationFailure,
    Cancel,
    BeginSettlement,
    RecordSettlementFailure,
    CompleteSettlement
}
