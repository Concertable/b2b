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

        var bookingConcert = await bookingRepository.GetByIdAsync(bookingId);
        if (bookingConcert is null)
            return Result.Failure<ConcertEntity, CreateConcertDraftError>(CreateConcertDraftError.NotFound(bookingId));

        var artist = bookingConcert.Application.Artist;
        var opportunity = bookingConcert.Application.Opportunity;
        var venue = opportunity.Venue;

        var artistGenres = artist.Genres.Select(g => g.Genre);
        var opportunityGenres = opportunity.Genres;

        var matchingGenres = opportunityGenres.Any()
            ? artistGenres.Intersect(opportunityGenres)
            : artistGenres;

        if (!matchingGenres.Any())
        {
            logger.ConcertDraftCreationFailed(bookingId, artist.Id, opportunity.Id);
            return Result.Failure<ConcertEntity, CreateConcertDraftError>(CreateConcertDraftError.GenreMismatch);
        }

        var concert = ConcertEntity.CreateDraft(
            bookingConcert.Id,
            artist.Id,
            venue.Id,
            opportunity.Period,
            $"{artist.Name} performing at {venue.Name}",
            venue.About,
            bookingConcert.DealType,
            matchingGenres);

        concert.VenueTenantId = bookingConcert.VenueTenantId;
        concert.ArtistTenantId = bookingConcert.ArtistTenantId;

        bookingConcert.Confirm(concert);
        await bookingRepository.SaveChangesAsync();

        logger.ConcertDraftCreated(concert.Id, bookingId, artist.Id, venue.Id);

        await notifier.ConcertDraftCreatedAsync(artist.UserId.ToString(), concert.Id);
        await notifier.ConcertDraftCreatedAsync(venue.UserId.ToString(), concert.Id);

        return Result.Success<ConcertEntity, CreateConcertDraftError>(concert);
    }
}
