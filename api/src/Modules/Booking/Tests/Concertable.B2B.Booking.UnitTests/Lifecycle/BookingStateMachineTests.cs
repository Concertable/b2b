using Concertable.B2B.Booking.Domain.Lifecycle;

namespace Concertable.B2B.Booking.UnitTests;

public sealed class BookingStateMachineTests
{
    [Fact]
    public void Transition_CoversEveryStateAndTrigger()
    {
        var expected = new Dictionary<(BookingState, BookingTrigger), BookingState>
        {
            [(BookingState.AwaitingConfirmation, BookingTrigger.Confirm)] = BookingState.Confirmed,
            [(BookingState.ConfirmationFailed, BookingTrigger.Confirm)] = BookingState.Confirmed,
            [(BookingState.AwaitingConfirmation, BookingTrigger.RecordConfirmationFailure)] = BookingState.ConfirmationFailed,
            [(BookingState.ConfirmationFailed, BookingTrigger.RecordConfirmationFailure)] = BookingState.ConfirmationFailed,
            [(BookingState.AwaitingConfirmation, BookingTrigger.BeginCancellation)] = BookingState.CancellationPending,
            [(BookingState.ConfirmationFailed, BookingTrigger.BeginCancellation)] = BookingState.CancellationPending,
            [(BookingState.CancellationFailed, BookingTrigger.BeginCancellation)] = BookingState.CancellationPending,
            [(BookingState.CancellationPending, BookingTrigger.RecordCancellationFailure)] = BookingState.CancellationFailed,
            [(BookingState.CancellationPending, BookingTrigger.Cancel)] = BookingState.Cancelled,
            [(BookingState.CancellationFailed, BookingTrigger.Cancel)] = BookingState.Cancelled
        };
        var machine = new BookingStateMachine();

        foreach (var state in Enum.GetValues<BookingState>())
        foreach (var trigger in Enum.GetValues<BookingTrigger>())
        {
            var result = machine.Transition(state, trigger);
            if (expected.TryGetValue((state, trigger), out var next))
            {
                Assert.True(result.TryGetValue(out var actual));
                Assert.Equal(next, actual);
            }
            else
            {
                Assert.True(result.TryGetError(out var error));
                Assert.Equal(state, error.Current);
                Assert.Equal(trigger, error.Trigger);
            }
        }
    }
}
