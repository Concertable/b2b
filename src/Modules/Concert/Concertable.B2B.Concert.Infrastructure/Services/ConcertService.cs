using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Contracts.Commands;
using Concertable.B2B.Concert.Contracts.Events;
using Concertable.B2B.Concert.Domain.State;
using Concertable.B2B.Tenant.Contracts;
using Concertable.Kernel.Identity;
using Concertable.Messaging.Contracts;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class ConcertService : IConcertService
{
    private readonly IConcertRepository repository;
    private readonly IConcertReadRepository readRepository;
    private readonly IInvoiceRepository invoiceRepository;
    private readonly IConcertValidator concertValidator;
    private readonly IArtistReadModelRepository artists;
    private readonly IVenueReadModelRepository venues;
    private readonly IBookingConfirmationEmailSender emailSender;
    private readonly IBus bus;
    private readonly IBookingModule bookingModule;
    private readonly TimeProvider timeProvider;
    private readonly ITenantContext tenantContext;
    private readonly ILogger<ConcertService> logger;

    public ConcertService(
        IConcertRepository repository,
        IConcertReadRepository readRepository,
        IInvoiceRepository invoiceRepository,
        IConcertValidator concertValidator,
        IArtistReadModelRepository artists,
        IVenueReadModelRepository venues,
        IBookingConfirmationEmailSender emailSender,
        IBus bus,
        IBookingModule bookingModule,
        TimeProvider timeProvider,
        ITenantContext tenantContext,
        ILogger<ConcertService> logger)
    {
        this.repository = repository;
        this.readRepository = readRepository;
        this.invoiceRepository = invoiceRepository;
        this.concertValidator = concertValidator;
        this.artists = artists;
        this.venues = venues;
        this.emailSender = emailSender;
        this.bus = bus;
        this.bookingModule = bookingModule;
        this.timeProvider = timeProvider;
        this.tenantContext = tenantContext;
        this.logger = logger;
    }

    public async Task CreateAsync(ConfirmedBooking booking, CancellationToken ct = default)
    {
        logger.CreatingConcertDraft(booking.BookingId);

        if (await repository.GetByBookingIdAsync(booking.BookingId, ct) is not null)
            return;

        var artist = await artists.GetByTenantIdAsync(booking.ArtistTenantId, ct)
            ?? throw new InvalidOperationException(
                $"Artist projection {booking.ArtistTenantId} was not found for booking {booking.BookingId}.");
        var venue = await venues.GetByTenantIdAsync(booking.VenueTenantId, ct)
            ?? throw new InvalidOperationException(
                $"Venue projection {booking.VenueTenantId} was not found for booking {booking.BookingId}.");
        if (artist.Id != booking.ArtistId || venue.Id != booking.VenueId)
            throw new InvalidOperationException(
                $"Booking {booking.BookingId} does not match its artist or venue projection.");

        var artistGenres = artist.Genres.Select(genre => genre.Genre);
        var matchingGenres = booking.Genres.Count > 0
            ? artistGenres.Intersect(booking.Genres)
            : artistGenres;
        if (!matchingGenres.Any())
        {
            logger.ConcertDraftCreationFailed(booking.BookingId, artist.Id, booking.OpportunityId);
            throw new InvalidOperationException(
                $"Artist {artist.Id} does not match the genres for booking {booking.BookingId}.");
        }

        var concert = ConcertEntity.CreateDraft(
            booking,
            $"{artist.Name} performing at {venue.Name}",
            venue.About,
            matchingGenres);
        await repository.AddAsync(concert, ct);
        await repository.SaveChangesAsync(ct);

        await bus.PublishAsync(new ConcertCreatedEvent(
            concert.Id,
            concert.ApplicationId,
            concert.OpportunityId,
            concert.ArtistId,
            concert.VenueId,
            concert.VenueTenantId,
            concert.ArtistTenantId,
            concert.Period.Start), ct);

        logger.ConcertDraftCreated(concert.Id, booking.BookingId, artist.Id, venue.Id);
        await bus.SendAsync(new NotifyConcertDraftCreatedCommand(
            concert.Id,
            artist.UserId,
            venue.UserId), ct);
        await emailSender.SendAsync(booking, venue.Name, artist.Name, ct);
    }

    public async Task<IReadOnlyList<ConcertSummary>> GetUpcomingByVenueIdAsync(int id) =>
        (await readRepository.GetUpcomingByVenueIdAsync(id)).ToList();

    public async Task<IReadOnlyList<ConcertSummary>> GetUpcomingByArtistIdAsync(int id) =>
        (await readRepository.GetUpcomingByArtistIdAsync(id)).ToList();

    public async Task<Result<IReadOnlyList<ManagerConcertCard>, ConcertError>> GetUpcomingForCurrentVenueAsync()
    {
        if (tenantContext.TenantId is not { } tenantId)
            return new ConcertError.MissingVenue();

        return new Success<IReadOnlyList<ManagerConcertCard>>(
            await repository.GetUpcomingCardsForVenueTenantIdAsync(tenantId));
    }

    public async Task<Result<IReadOnlyList<ManagerConcertCard>, ConcertError>> GetUpcomingForCurrentArtistAsync()
    {
        if (tenantContext.TenantId is not { } tenantId)
            return new ConcertError.MissingArtist();

        return new Success<IReadOnlyList<ManagerConcertCard>>(
            await repository.GetUpcomingCardsForArtistTenantIdAsync(tenantId));
    }

    public async Task<IReadOnlyList<ConcertSummary>> GetHistoryByArtistIdAsync(int id) =>
        (await readRepository.GetHistoryByArtistIdAsync(id)).ToList();

    public async Task<IReadOnlyList<ConcertSummary>> GetHistoryByVenueIdAsync(int id) =>
        (await readRepository.GetHistoryByVenueIdAsync(id)).ToList();

    public Task<Result<ConcertDetails, ConcertError>> GetDetailsByIdAsync(int id) =>
        readRepository.GetDetailsByIdAsync(id)
            .ToOption()
            .OrFailure(() => (ConcertError)new ConcertError.NotFound(id));

    public async Task<Result<ConcertDetails, ConcertError>> GetDetailsAsync(
        int id,
        CancellationToken ct = default)
    {
        return await repository.GetDetailsByIdAsync(id, ct)
            .ToOption()
            .OrFailure(() => (ConcertError)new ConcertError.NotFound(id))
            .MapAsync(async details =>
            {
                var invoice = await invoiceRepository.GetByConcertIdAsync(id, ct);
                return WithActions(details with { InvoiceId = invoice?.Id });
            });
    }

    public async Task<Result<FileDownload, ConcertError>> GetContractPdfAsync(
        int id,
        CancellationToken ct = default)
    {
        var concert = await repository.GetByIdAsync(id, ct);
        if (concert is null)
            return new ConcertError.NotFound(id);

        var contractPdf = await bookingModule.GetContractPdfByBookingIdAsync(concert.BookingId, ct);
        return contractPdf.TryGetValue(out var pdf)
            ? new FileDownload(pdf.Content, pdf.FileName, pdf.ContentType)
            : new ConcertError.NotFound(id);
    }

    public async Task<Result<ConcertDetails, ConcertError>> GetDetailsByApplicationIdAsync(int applicationId)
    {
        return await repository.GetDetailsByApplicationIdAsync(applicationId)
            .ToOption()
            .OrFailure(() => (ConcertError)new ConcertError.ApplicationNotFound(applicationId))
            .MapAsync(async details =>
            {
                var invoice = await invoiceRepository.GetByApplicationIdAsync(applicationId);
                return WithActions(details with { InvoiceId = invoice?.Id });
            });
    }

    public async Task<Result<ConcertUpdateResponse, UpdateConcertError>> UpdateAsync(int id, UpdateConcertRequest request)
    {
        var concertEntity = await repository.GetByIdAsync(id);
        if (concertEntity is null)
            return new UpdateConcertError.ConcertNotFound(id);

        var result = concertValidator.CanUpdate(concertEntity, request.TotalTickets);
        if (result.TryGetErrors(out var errors))
            return new UpdateConcertError.Invalid(new ValidationErrors(errors.ToDictionary()));

        concertEntity.Update(request.Name, request.About, request.Price, request.TotalTickets);

        await repository.SaveChangesAsync();

        return new ConcertUpdateResponse
        {
            Id = concertEntity.Id,
            Name = concertEntity.Name,
            About = concertEntity.About,
            Price = concertEntity.Price,
            TotalTickets = concertEntity.TotalTickets,
            AvailableTickets = 0 // moved to Customer.Concert; UI reads via Search projection in end-state
        };
    }

    public async Task<UnitResult<PostConcertError>> PostAsync(int id, UpdateConcertRequest request)
    {
        var concertEntity = await repository.GetByIdAsync(id);
        if (concertEntity is null)
            return new PostConcertError.ConcertNotFound(id);

        var result = concertValidator.CanPost(concertEntity);
        if (result.TryGetErrors(out var errors))
            return new PostConcertError.Invalid(new ValidationErrors(errors.ToDictionary()));

        concertEntity.Post(request.Name, request.About, request.Price, request.TotalTickets, timeProvider.GetUtcNow().DateTime);

        await repository.SaveChangesAsync();
        return new Success();
    }

    public async Task<UnitResult<DeclareDoorRevenueError>> DeclareDoorRevenueAsync(int id, decimal doorRevenue)
    {
        var concert = await repository.GetByIdAsync(id);
        if (concert is null)
            return new DeclareDoorRevenueError.ConcertNotFound(id);

        if (!tenantContext.IsHost && concert.VenueTenantId != tenantContext.TenantId)
            return new DeclareDoorRevenueError.VenueForbidden();

        if (!concert.RequiresDoorRevenue)
            return new DeclareDoorRevenueError.WrongDealType();
        if (timeProvider.GetUtcNow().UtcDateTime < concert.Period.End)
            return new DeclareDoorRevenueError.TooEarly();
        if (concert.State is not (ConcertState.Draft or ConcertState.Posted))
            return new DeclareDoorRevenueError.AlreadySettled();

        return await concert.DeclareDoorRevenue(doorRevenue)
            .MapError(error => error.ToDeclareDoorRevenueError())
            .TapAsync(() => repository.SaveChangesAsync());
    }

    public async Task<IReadOnlyList<ConcertSummary>> GetUnpostedByArtistIdAsync(int id) =>
        (await repository.GetUnpostedByArtistIdAsync(id)).ToList();

    public async Task<IReadOnlyList<ConcertSummary>> GetUnpostedByVenueIdAsync(int id) =>
        (await repository.GetUnpostedByVenueIdAsync(id)).ToList();

    private ConcertDetails WithActions(ConcertDetails details) => details with
    {
        CanCancel = details.State is ConcertState.Draft or ConcertState.Posted or ConcertState.CancellationFailed,
        CanDeclareDoorRevenue = details.State is ConcertState.Draft or ConcertState.Posted
            && details.IsRevenueShare
            && details.DoorRevenue is null
            && details.EndDate < timeProvider.GetUtcNow().UtcDateTime
    };
}
