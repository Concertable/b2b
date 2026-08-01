using Dunet;

namespace Concertable.B2B.Concert.Application.Errors;

[Union]
internal partial record CreateConcertDraftError : IError
{
    partial record BookingNotFound(int BookingId);
    partial record GenreMismatch;

    public static CreateConcertDraftError NotFound(int bookingId) => new BookingNotFound(bookingId);

    public static CreateConcertDraftError InvalidGenres() => new GenreMismatch();

    public ErrorDefinition Definition => Match<ErrorDefinition>(
        error => ErrorDefinition.NotFound(
            "concert.draft.booking_not_found",
            $"Booking {error.BookingId} was not found."),
        _ => ErrorDefinition.Invalid(
            "concert.draft.genre_mismatch",
            "The artist does not match any genres required by the concert opportunity."));
}
