using Concertable.B2B.Concert.Domain.Entities;
using Concertable.Kernel.Exceptions;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal class ConcertDraftService : IConcertDraftService
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

    public async Task<Result<ConcertEntity>> CreateAsync(int bookingId)
    {
        logger.LogInformation("Creating concert draft for booking {BookingId}", bookingId);

        var bookingConcert = await bookingRepository.GetByIdAsync(bookingId)
            ?? throw new NotFoundException("Booking not found");

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
            logger.LogWarning(
                "Concert draft creation failed for booking {BookingId}: artist {ArtistId} has no matching genres for opportunity {OpportunityId}",
                bookingId, artist.Id, opportunity.Id);
            return Result.Fail("The artist does not match any genres required by the concert opportunity");
        }

        var concert = ConcertEntity.CreateDraft(
            bookingConcert.Id,
            artist.Id,
            venue.Id,
            opportunity.Period,
            $"{artist.Name} performing at {venue.Name}",
            venue.About,
            matchingGenres);

        bookingConcert.Confirm(concert);
        await bookingRepository.SaveChangesAsync();

        logger.LogInformation(
            "Concert draft {ConcertId} created for booking {BookingId} (artist {ArtistId}, venue {VenueId}); notifying users",
            concert.Id, bookingId, artist.Id, venue.Id);

        await notifier.ConcertDraftCreatedAsync(artist.UserId.ToString(), concert.Id);
        await notifier.ConcertDraftCreatedAsync(venue.UserId.ToString(), concert.Id);

        return Result.Ok(concert);
    }
}
