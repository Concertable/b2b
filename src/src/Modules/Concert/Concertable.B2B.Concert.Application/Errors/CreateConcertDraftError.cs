using Dunet;

namespace Concertable.B2B.Concert.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record CreateConcertDraftError : IError
{
    public ErrorDefinition Definition => this switch
    {
        BookingNotFound(var bookingId) =>
            ErrorDefinition.NotFound<BookingNotFound>(
                $"Booking {bookingId} was not found."),
        GenreMismatch =>
            ErrorDefinition.Invalid<GenreMismatch>(
                "The artist does not match any genres required by the concert opportunity.")
    };

    [ErrorCode("concert.draft.booking_not_found")]
    public partial record BookingNotFound(int BookingId);

    [ErrorCode("concert.draft.genre_mismatch")]
    public partial record GenreMismatch;
}
