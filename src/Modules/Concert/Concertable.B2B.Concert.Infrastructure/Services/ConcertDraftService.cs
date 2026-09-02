using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class ConcertDraftService : IConcertDraftService
{
    private readonly IBookingRepository bookingRepository;
    private readonly IConcertNotifier notifier;
    private readonly ILogger<ConcertDraftService> logger;

    public ConcertDraftService(
        IBookingRepository bookingRepository,
        IConcertNotifier notifier,
        ILogger<ConcertDraftService> logger)
    {
        this.bookingRepository = bookingRepository;
        this.notifier = notifier;
        this.logger = logger;
    }

    public async Task<Result<ConcertEntity, CreateConcertDraftError>> CreateAsync(int bookingId)
    {
        logger.CreatingConcertDraft(bookingId);

        var bookingConcert = await bookingRepository.GetWithApplicationAndConcertByIdAsync(bookingId);
        if (bookingConcert is null)
            return new CreateConcertDraftError.BookingNotFound(bookingId);

        var artist = bookingConcert.Application.Artist;
        var opportunity = bookingConcert.Application.Opportunity;
        var venue = opportunity.Venue;

        var artistGenres = artist.Genres.Select(g => g.Genre).ToList();
        var opportunityGenres = opportunity.Genres;

        var matchingGenres = opportunityGenres.Count > 0
            ? artistGenres.Intersect(opportunityGenres).ToList()
            : artistGenres;

        if (matchingGenres.Count == 0)
        {
            logger.ConcertDraftCreationFailed(bookingId, artist.Id, opportunity.Id);
            return new CreateConcertDraftError.GenreMismatch();
        }

        var concert = ConcertEntity.CreateDraft(
            bookingConcert,
            artist.Id,
            venue.Id,
            opportunity.Period,
            $"{artist.Name} performing at {venue.Name}",
            venue.About,
            matchingGenres);

        bookingConcert.Confirm(concert, venue.Name, artist.Name);
        await bookingRepository.SaveChangesAsync();

        logger.ConcertDraftCreated(concert.Id, bookingId, artist.Id, venue.Id);

        await notifier.ConcertDraftCreatedAsync(artist.UserId.ToString(), concert.Id);
        await notifier.ConcertDraftCreatedAsync(venue.UserId.ToString(), concert.Id);

        return concert;
    }
}
