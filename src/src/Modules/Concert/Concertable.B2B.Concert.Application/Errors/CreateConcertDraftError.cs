namespace Concertable.B2B.Concert.Application.Errors;

internal sealed record CreateConcertDraftError(ErrorDefinition Definition) : IError
{
    internal static CreateConcertDraftError NotFound(int bookingId) =>
        new(ErrorDefinition.NotFound(
            "concert.draft.booking_not_found",
            $"Booking {bookingId} was not found."));

    internal static readonly CreateConcertDraftError GenreMismatch = new(
        ErrorDefinition.Invalid(
            "concert.draft.genre_mismatch",
            "The artist does not match any genres required by the concert opportunity."));
}
