namespace Concertable.B2B.Concert.Domain.Lifecycle;

internal sealed class ConcertStateMachine() : Concertable.Kernel.StateMachine<ConcertState, ConcertTrigger>(
[
    (ConcertState.Draft, ConcertTrigger.Post, ConcertState.Posted),
    (ConcertState.Posted, ConcertTrigger.Post, ConcertState.Posted),
    (ConcertState.Draft, ConcertTrigger.BeginCancellation, ConcertState.CancellationPending),
    (ConcertState.Posted, ConcertTrigger.BeginCancellation, ConcertState.CancellationPending),
    (ConcertState.CancellationFailed, ConcertTrigger.BeginCancellation, ConcertState.CancellationPending),
    (ConcertState.CancellationPending, ConcertTrigger.RecordCancellationFailure, ConcertState.CancellationFailed),
    (ConcertState.CancellationPending, ConcertTrigger.Cancel, ConcertState.Cancelled),
    (ConcertState.CancellationFailed, ConcertTrigger.Cancel, ConcertState.Cancelled),
    (ConcertState.Draft, ConcertTrigger.BeginSettlement, ConcertState.AwaitingSettlement),
    (ConcertState.Posted, ConcertTrigger.BeginSettlement, ConcertState.AwaitingSettlement),
    (ConcertState.SettlementFailed, ConcertTrigger.BeginSettlement, ConcertState.AwaitingSettlement),
    (ConcertState.AwaitingSettlement, ConcertTrigger.RecordSettlementFailure, ConcertState.SettlementFailed),
    (ConcertState.Draft, ConcertTrigger.CompleteSettlement, ConcertState.Complete),
    (ConcertState.Posted, ConcertTrigger.CompleteSettlement, ConcertState.Complete),
    (ConcertState.AwaitingSettlement, ConcertTrigger.CompleteSettlement, ConcertState.Complete),
    (ConcertState.SettlementFailed, ConcertTrigger.CompleteSettlement, ConcertState.Complete)
]);
