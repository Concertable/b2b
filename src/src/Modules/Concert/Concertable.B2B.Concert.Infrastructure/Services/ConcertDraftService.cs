using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.Kernel.Exceptions;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class ConcertDraftService : IConcertDraftService
{
    private readonly IBookingRepository bookingRepository;
    private readonly IConcertNotifier notifier;
    private readonly IBookingConfirmationNotifier bookingConfirmationNotifier;
    private readonly ILogger<ConcertDraftService> logger;

    public ConcertDraftService(
        IBookingRepository bookingRepository,
        IConcertNotifier notifier,
        IBookingConfirmationNotifier bookingConfirmationNotifier,
        ILogger<ConcertDraftService> logger)
    {
        this.bookingRepository = bookingRepository;
        this.notifier = notifier;
        this.bookingConfirmationNotifier = bookingConfirmationNotifier;
        this.logger = logger;
    }

    public async Task<Result<ConcertEntity>> CreateAsync(int bookingId)
    {
        logger.CreatingConcertDraft(bookingId);

        var bookingConcert = await bookingRepository.GetByIdAsync(bookingId)
            .OrNotFound();

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
            return Result.Fail("The artist does not match any genres required by the concert opportunity");
        }

        var concert = ConcertEntity.CreateDraft(
            bookingConcert,
            artist.Id,
            venue.Id,
            opportunity.Period,
            $"{artist.Name} performing at {venue.Name}",
            venue.About,
            matchingGenres);

        bookingConcert.Confirm(concert);
        await bookingRepository.SaveChangesAsync();

        logger.ConcertDraftCreated(concert.Id, bookingId, artist.Id, venue.Id);

        await notifier.ConcertDraftCreatedAsync(artist.UserId.ToString(), concert.Id);
        await notifier.ConcertDraftCreatedAsync(venue.UserId.ToString(), concert.Id);

        await bookingConfirmationNotifier.BookingConfirmedAsync(
            bookingConcert.Application.VenueTenantId, venue.Name,
            bookingConcert.Application.ArtistTenantId, artist.Name,
            concert.Period);

        return Result.Ok(concert);
    }
}
