using Concertable.B2B.Concert.Domain.Entities;
using Concertable.Kernel.Identity;
using Concertable.Shared.Email.Application;
using Concertable.Kernel.Exceptions;
using FluentResults;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class ConcertService : IConcertService
{
    private readonly IConcertRepository repository;
    private readonly IPublicConcertRepository publicRepository;
    private readonly IConcertValidator concertValidator;
    private readonly ICurrentUser currentUser;
    private readonly IApplicationValidator applicationValidator;
    private readonly IEmailSender emailSender;
    private readonly IConcertDraftService concertDraftService;
    private readonly TimeProvider timeProvider;

    public ConcertService(
        IConcertRepository repository,
        IPublicConcertRepository publicRepository,
        IConcertValidator concertValidator,
        ICurrentUser currentUser,
        IApplicationValidator applicationValidator,
        IEmailSender emailSender,
        IConcertDraftService concertDraftService,
        TimeProvider timeProvider)
    {
        this.repository = repository;
        this.publicRepository = publicRepository;
        this.concertValidator = concertValidator;
        this.currentUser = currentUser;
        this.applicationValidator = applicationValidator;
        this.emailSender = emailSender;
        this.concertDraftService = concertDraftService;
        this.timeProvider = timeProvider;
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
            ?? throw new NotFoundException("Concert not found");
    }

    public Task<Result<ConcertEntity>> CreateDraftAsync(int applicationId) =>
        concertDraftService.CreateAsync(applicationId);

    public async Task<ConcertDetails> GetDetailsByApplicationIdAsync(int applicationId)
    {
        return await repository.GetDetailsByApplicationIdAsync(applicationId)
            ?? throw new NotFoundException($"No concert found for Application ID {applicationId}");
    }

    public async Task<ConcertUpdateResponse> UpdateAsync(int id, UpdateConcertRequest request)
    {
        var concertEntity = await repository.GetByIdAsync(id)
            ?? throw new NotFoundException("Concert not found");

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
            ?? throw new NotFoundException("Concert not found");

        var result = concertValidator.CanPost(concertEntity);
        if (result.IsFailed)
            throw new BadRequestException(result.Errors);

        concertEntity.Post(request.Name, request.About, request.Price, request.TotalTickets, timeProvider.GetUtcNow().DateTime);

        await repository.SaveChangesAsync();
    }

    public Task<IEnumerable<ConcertSummary>> GetUnpostedByArtistIdAsync(int id) =>
        repository.GetUnpostedByArtistIdAsync(id);

    public Task<IEnumerable<ConcertSummary>> GetUnpostedByVenueIdAsync(int id) =>
        repository.GetUnpostedByVenueIdAsync(id);
}
