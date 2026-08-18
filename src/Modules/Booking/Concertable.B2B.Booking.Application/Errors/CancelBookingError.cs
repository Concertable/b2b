using Concertable.B2B.Booking.Domain.State;
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
        InvalidState(var state) => ErrorDefinition.Conflict<InvalidState>(
            $"A booking in {state} cannot be cancelled through the application endpoint.")
    };

    [ErrorCode("booking.cancel.not_found")]
    public partial record BookingNotFound(int BookingId);

    [ErrorCode("booking.cancel.invalid_state")]
    public partial record InvalidState(BookingState State);
}
