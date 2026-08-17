using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class ConcertDraftService : IConcertDraftService
{
    private readonly IBookingRepository bookingRepository;
    private readonly IConcertRepository concertRepository;
    private readonly IConcertNotifier notifier;
    private readonly ILogger<ConcertDraftService> logger;

    public ConcertDraftService(
        IBookingRepository bookingRepository,
        IConcertRepository concertRepository,
        IConcertNotifier notifier,
        ILogger<ConcertDraftService> logger)
    {
        this.bookingRepository = bookingRepository;
        this.concertRepository = concertRepository;
        this.notifier = notifier;
        this.logger = logger;
    }

    public async Task<Result<ConcertEntity, CreateConcertDraftError>> CreateAsync(int bookingId)
    {
        logger.CreatingConcertDraft(bookingId);

        var context = await bookingRepository.GetDraftContextByIdAsync(bookingId);
        if (context is null)
            return new CreateConcertDraftError.BookingNotFound(bookingId);

        var booking = context.Booking;
        var artist = context.Artist;
        var opportunity = context.Opportunity;
        var venue = context.Venue;

        var artistGenres = artist.Genres.Select(g => g.Genre);
        var opportunityGenres = opportunity.Genres;

        var matchingGenres = opportunityGenres.Any()
            ? artistGenres.Intersect(opportunityGenres)
            : artistGenres;

        if (!matchingGenres.Any())
        {
            logger.ConcertDraftCreationFailed(bookingId, artist.Id, opportunity.Id);
            return new CreateConcertDraftError.GenreMismatch();
        }

        var concert = ConcertEntity.CreateDraft(
            new ConfirmedBooking(
                booking.OperationId,
                booking.Id,
                booking.ApplicationId,
                artist.Id,
                venue.Id,
                booking.VenueTenantId,
                booking.ArtistTenantId,
                booking.DealType,
                booking is DeferredBooking,
                opportunity.Period.Start,
                opportunity.Period.End),
            $"{artist.Name} performing at {venue.Name}",
            venue.About,
            matchingGenres);

        await concertRepository.AddAsync(concert);
        booking.Confirm(concert.Period, venue.Name, artist.Name);
        await concertRepository.SaveChangesAsync();

        logger.ConcertDraftCreated(concert.Id, bookingId, artist.Id, venue.Id);

        await notifier.ConcertDraftCreatedAsync(artist.UserId.ToString(), concert.Id);
        await notifier.ConcertDraftCreatedAsync(venue.UserId.ToString(), concert.Id);

        return concert;
    }
}
