using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.Kernel.Identity;
using Concertable.Shared.Email.Application;
using Concertable.Kernel.Exceptions;
using FluentResults;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class ConcertService : IConcertService
{
    private readonly IConcertRepository repository;
    private readonly IPublicConcertRepository publicRepository;
    private readonly IInvoiceRepository invoiceRepository;
    private readonly IConcertValidator concertValidator;
    private readonly ICurrentUser currentUser;
    private readonly IApplicationValidator applicationValidator;
    private readonly IEmailSender emailSender;
    private readonly IConcertDraftService concertDraftService;
    private readonly TimeProvider timeProvider;
    private readonly ITenantContext tenantContext;

    public ConcertService(
        IConcertRepository repository,
        IPublicConcertRepository publicRepository,
        IInvoiceRepository invoiceRepository,
        IConcertValidator concertValidator,
        ICurrentUser currentUser,
        IApplicationValidator applicationValidator,
        IEmailSender emailSender,
        IConcertDraftService concertDraftService,
        TimeProvider timeProvider,
        ITenantContext tenantContext)
    {
        this.repository = repository;
        this.publicRepository = publicRepository;
        this.invoiceRepository = invoiceRepository;
        this.concertValidator = concertValidator;
        this.currentUser = currentUser;
        this.applicationValidator = applicationValidator;
        this.emailSender = emailSender;
        this.concertDraftService = concertDraftService;
        this.timeProvider = timeProvider;
        this.tenantContext = tenantContext;
    }

    public Task<IEnumerable<ConcertSummary>> GetUpcomingByVenueIdAsync(int id) =>
        publicRepository.GetUpcomingByVenueIdAsync(id);

    public Task<IEnumerable<ConcertSummary>> GetUpcomingByArtistIdAsync(int id) =>
        publicRepository.GetUpcomingByArtistIdAsync(id);

    public Task<IEnumerable<ConcertSummary>> GetHistoryByArtistIdAsync(int id) =>
        publicRepository.GetHistoryByArtistIdAsync(id);

    public Task<IEnumerable<ConcertSummary>> GetHistoryByVenueIdAsync(int id) =>
        publicRepository.GetHistoryByVenueIdAsync(id);

    public async Task<ConcertDetails> GetDetailsByIdAsync(int id)
    {
        return await publicRepository.GetDetailsByIdAsync(id)
            .OrNotFound();
    }

    public async Task<ConcertDetails> GetDetailsForCurrentUserAsync(int id)
    {
        var details = await repository.GetDetailsByIdAsync(id)
            .OrNotFound();
        var invoice = await invoiceRepository.GetByConcertIdAsync(id);
        return details with { InvoiceId = invoice?.Id };
    }

    public Task<Result<ConcertEntity>> CreateDraftAsync(int applicationId) =>
        concertDraftService.CreateAsync(applicationId);

    public async Task<ConcertDetails> GetDetailsByApplicationIdAsync(int applicationId)
    {
        var details = await repository.GetDetailsByApplicationIdAsync(applicationId)
            ?? throw new NotFoundException($"No concert found for Application ID {applicationId}");
        var invoice = await invoiceRepository.GetByApplicationIdAsync(applicationId);
        return details with { InvoiceId = invoice?.Id };
    }

    public async Task<ConcertUpdateResponse> UpdateAsync(int id, UpdateConcertRequest request)
    {
        var concertEntity = await repository.GetByIdAsync(id)
            .OrNotFound();

        var result = concertValidator.CanUpdate(concertEntity, request.TotalTickets);
        if (result.IsFailed)
            throw new BadRequestException(result.Errors);

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

    public async Task PostAsync(int id, UpdateConcertRequest request)
    {
        var concertEntity = await repository.GetByIdWithBookingAsync(id)
            .OrNotFound();

        var result = concertValidator.CanPost(concertEntity);
        if (result.IsFailed)
            throw new BadRequestException(result.Errors);

        concertEntity.Post(request.Name, request.About, request.Price, request.TotalTickets, timeProvider.GetUtcNow().DateTime);

        await repository.SaveChangesAsync();
    }

    public async Task DeclareDoorRevenueAsync(int id, decimal doorRevenue)
    {
        var concert = await repository.GetByIdWithBookingAsync(id)
            .OrNotFound();

        /* Only the concert's own venue may declare its door take. A non-party sees a null (tenant-filtered)
           Booking; the host/worker path (no HTTP context) bypasses tenant scoping, as elsewhere. */
        if (!tenantContext.IsHost && concert.Booking?.VenueTenantId != tenantContext.TenantId)
            throw new ForbiddenException("Only the concert's venue can declare its door revenue.");

        /* Only revenue-share settlements (DeferredBooking) take a declared door figure, and only once
           the gig has ended and before it settles. Re-declarable while Booked; frozen after. */
        if (concert.Booking is not DeferredBooking)
            throw new BadRequestException("Door revenue can only be declared for a revenue-share concert.");
        if (timeProvider.GetUtcNow().UtcDateTime < concert.Period.End)
            throw new BadRequestException("Door revenue can only be declared after the concert has ended.");
        if (concert.Booking.Application.State != LifecycleState.Booked)
            throw new ConflictException("Door revenue can only be declared before the concert has settled.");

        concert.DeclareDoorRevenue(doorRevenue);
        await repository.SaveChangesAsync();
    }

    public Task<IEnumerable<ConcertSummary>> GetUnpostedByArtistIdAsync(int id) =>
        repository.GetUnpostedByArtistIdAsync(id);

    public Task<IEnumerable<ConcertSummary>> GetUnpostedByVenueIdAsync(int id) =>
        repository.GetUnpostedByVenueIdAsync(id);
}
