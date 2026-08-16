using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.Kernel.Identity;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class ConcertService : IConcertService
{
    private readonly IConcertRepository repository;
    private readonly IPublicConcertRepository publicRepository;
    private readonly IInvoiceRepository invoiceRepository;
    private readonly IConcertValidator concertValidator;
    private readonly ICurrentUser currentUser;
    private readonly IApplicationValidator applicationValidator;
    private readonly IConcertDraftService concertDraftService;
    private readonly ICancelExecutor cancelExecutor;
    private readonly TimeProvider timeProvider;
    private readonly ITenantContext tenantContext;

    public ConcertService(
        IConcertRepository repository,
        IPublicConcertRepository publicRepository,
        IInvoiceRepository invoiceRepository,
        IConcertValidator concertValidator,
        ICurrentUser currentUser,
        IApplicationValidator applicationValidator,
        IConcertDraftService concertDraftService,
        ICancelExecutor cancelExecutor,
        TimeProvider timeProvider,
        ITenantContext tenantContext)
    {
        this.repository = repository;
        this.publicRepository = publicRepository;
        this.invoiceRepository = invoiceRepository;
        this.concertValidator = concertValidator;
        this.currentUser = currentUser;
        this.applicationValidator = applicationValidator;
        this.concertDraftService = concertDraftService;
        this.cancelExecutor = cancelExecutor;
        this.timeProvider = timeProvider;
        this.tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<ConcertSummary>> GetUpcomingByVenueIdAsync(int id) =>
        (await publicRepository.GetUpcomingByVenueIdAsync(id)).ToList();

    public async Task<IReadOnlyList<ConcertSummary>> GetUpcomingByArtistIdAsync(int id) =>
        (await publicRepository.GetUpcomingByArtistIdAsync(id)).ToList();

    public async Task<IReadOnlyList<ConcertSummary>> GetHistoryByArtistIdAsync(int id) =>
        (await publicRepository.GetHistoryByArtistIdAsync(id)).ToList();

    public async Task<IReadOnlyList<ConcertSummary>> GetHistoryByVenueIdAsync(int id) =>
        (await publicRepository.GetHistoryByVenueIdAsync(id)).ToList();

    public Task<Result<ConcertDetails, ConcertError>> GetDetailsByIdAsync(int id) =>
        publicRepository.GetDetailsByIdAsync(id)
            .ToOption()
            .OrFailure(() => (ConcertError)new ConcertError.NotFound(id));

    public async Task<Result<ConcertDetails, ConcertError>> GetDetailsForActiveTenantAsync(
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

    public Task<Result<ConcertEntity, CreateConcertDraftError>> CreateDraftAsync(int applicationId) =>
        concertDraftService.CreateAsync(applicationId);

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
        var concertEntity = await repository.GetByIdWithBookingAsync(id);
        if (concertEntity is null)
            return new PostConcertError.ConcertNotFound(id);

        var result = concertValidator.CanPost(concertEntity);
        if (result.TryGetErrors(out var errors))
            return new PostConcertError.Invalid(new ValidationErrors(errors.ToDictionary()));

        concertEntity.Post(request.Name, request.About, request.Price, request.TotalTickets, timeProvider.GetUtcNow().DateTime);

        await repository.SaveChangesAsync();
        return new Success();
    }

    public Task<UnitResult<CancelConcertError>> CancelAsync(int concertId, CancellationToken ct) =>
        cancelExecutor.CancelAsync(concertId, ct);

    public async Task<UnitResult<DeclareDoorRevenueError>> DeclareDoorRevenueAsync(int id, decimal doorRevenue)
    {
        var concert = await repository.GetByIdWithBookingAsync(id);
        if (concert is null)
            return new DeclareDoorRevenueError.ConcertNotFound(id);

        /* Only the concert's own venue may declare its door take. A non-party sees a null (tenant-filtered)
           Booking; the host/worker path (no HTTP context) bypasses tenant scoping, as elsewhere. */
        if (!tenantContext.IsHost && concert.Booking?.VenueTenantId != tenantContext.TenantId)
            return new DeclareDoorRevenueError.VenueForbidden();

        /* Only revenue-share settlements (DeferredBooking) take a declared door figure, and only once
           the gig has ended and before it settles. Re-declarable while Booked; frozen after. */
        if (concert.Booking is not DeferredBooking)
            return new DeclareDoorRevenueError.WrongDealType();
        if (timeProvider.GetUtcNow().UtcDateTime < concert.Period.End)
            return new DeclareDoorRevenueError.TooEarly();
        if (concert.Booking.Application.State != LifecycleState.Booked)
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
        CanCancel = details.State == LifecycleState.Booked,
        CanDeclareDoorRevenue = details.State == LifecycleState.Booked
            && details.IsRevenueShare
            && details.DoorRevenue is null
            && details.EndDate < timeProvider.GetUtcNow().UtcDateTime
    };
}
