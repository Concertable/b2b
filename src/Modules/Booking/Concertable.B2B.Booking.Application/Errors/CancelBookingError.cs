using Concertable.B2B.Booking.Domain.Lifecycle;
using Concertable.Kernel;
using Dunet;
using Reunion.Errors;

namespace Concertable.B2B.Booking.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record CancelBookingError : IError
{
    public ErrorDefinition Definition => this switch
    {
        BookingNotFound(var bookingId) => ErrorDefinition.NotFound<BookingNotFound>(
            $"Booking {bookingId} was not found."),
        InvalidTransition(var error) => ErrorDefinition.Conflict<InvalidTransition>(
            $"A booking in {error.Current} cannot be cancelled.")
    };

    [ErrorCode("booking.cancel.not_found")]
    public partial record BookingNotFound(int BookingId);

    [ErrorCode("booking.cancel.invalid_state")]
    public partial record InvalidTransition(TransitionError<State, Trigger> Error);
}
