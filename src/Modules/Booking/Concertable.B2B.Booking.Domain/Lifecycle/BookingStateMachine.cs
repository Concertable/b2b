namespace Concertable.B2B.Booking.Domain.Lifecycle;

internal sealed class BookingStateMachine() : Concertable.Kernel.StateMachine<BookingState, BookingTrigger>(
[
    (BookingState.AwaitingConfirmation, BookingTrigger.Confirm, BookingState.Confirmed),
    (BookingState.ConfirmationFailed, BookingTrigger.Confirm, BookingState.Confirmed),
    (BookingState.AwaitingConfirmation, BookingTrigger.RecordConfirmationFailure, BookingState.ConfirmationFailed),
    (BookingState.ConfirmationFailed, BookingTrigger.RecordConfirmationFailure, BookingState.ConfirmationFailed),
    (BookingState.AwaitingConfirmation, BookingTrigger.BeginCancellation, BookingState.CancellationPending),
    (BookingState.ConfirmationFailed, BookingTrigger.BeginCancellation, BookingState.CancellationPending),
    (BookingState.CancellationFailed, BookingTrigger.BeginCancellation, BookingState.CancellationPending),
    (BookingState.CancellationPending, BookingTrigger.RecordCancellationFailure, BookingState.CancellationFailed),
    (BookingState.CancellationPending, BookingTrigger.Cancel, BookingState.Cancelled),
    (BookingState.CancellationFailed, BookingTrigger.Cancel, BookingState.Cancelled)
]);
