using Concertable.B2B.Concert.Domain.Entities;
using Concertable.Kernel.Identity;
using Concertable.Shared.Email.Application;
using Concertable.Kernel.Exceptions;
using FluentResults;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class ConcertService : IConcertService
{
    private readonly IConcertRepository concertRepository;
    private readonly IConcertValidator concertValidator;
    private readonly ICurrentUser currentUser;
    private readonly IApplicationValidator applicationValidator;
    private readonly IEmailSender emailSender;
    private readonly IConcertDraftService concertDraftService;
    private readonly TimeProvider timeProvider;

    public ConcertService(
        IConcertRepository concertRepository,
        IConcertValidator concertValidator,
        ICurrentUser currentUser,
        IApplicationValidator applicationValidator,
        IEmailSender emailSender,
        IConcertDraftService concertDraftService,
        TimeProvider timeProvider)
    {
        this.concertRepository = concertRepository;
        this.concertValidator = concertValidator;
        this.currentUser = currentUser;
        this.applicationValidator = applicationValidator;
        this.emailSender = emailSender;
        this.concertDraftService = concertDraftService;
        this.timeProvider = timeProvider;
    }

    public Task<IEnumerable<ConcertSummaryDto>> GetUpcomingByVenueIdAsync(int id) =>
        concertRepository.GetUpcomingByVenueIdAsync(id);

    public Task<IEnumerable<ConcertSummaryDto>> GetUpcomingByArtistIdAsync(int id) =>
        concertRepository.GetUpcomingByArtistIdAsync(id);

    public Task<IEnumerable<ConcertSummaryDto>> GetHistoryByArtistIdAsync(int id) =>
        concertRepository.GetHistoryByArtistIdAsync(id);

    public Task<IEnumerable<ConcertSummaryDto>> GetHistoryByVenueIdAsync(int id) =>
        concertRepository.GetHistoryByVenueIdAsync(id);

    public async Task<ConcertDto> GetDetailsByIdAsync(int id)
    {
        return await concertRepository.GetDtoByIdAsync(id)
            ?? throw new NotFoundException("Concert not found");
    }

    public Task<Result<ConcertEntity>> CreateDraftAsync(int applicationId) =>
        concertDraftService.CreateAsync(applicationId);

    public async Task<ConcertDto> GetDetailsByApplicationIdAsync(int applicationId)
    {
        return await concertRepository.GetDtoByApplicationIdAsync(applicationId)
            ?? throw new NotFoundException($"No concert found for Application ID {applicationId}");
    }

    public async Task<ConcertUpdateResponse> UpdateAsync(int id, UpdateConcertRequest request)
    {
        var concertEntity = await concertRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Concert not found");

        var result = concertValidator.CanUpdate(concertEntity, request.TotalTickets);
        if (result.IsFailed)
            throw new BadRequestException(result.Errors);

        concertEntity.Update(request.Name, request.About, request.Price, request.TotalTickets);

        await concertRepository.SaveChangesAsync();

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
        var concertEntity = await concertRepository.GetFullByIdAsync(id)
            ?? throw new NotFoundException("Concert not found");

        var result = concertValidator.CanPost(concertEntity);
        if (result.IsFailed)
            throw new BadRequestException(result.Errors);

        concertEntity.Post(request.Name, request.About, request.Price, request.TotalTickets, timeProvider.GetUtcNow().DateTime);

        await concertRepository.SaveChangesAsync();
    }

    public Task<IEnumerable<ConcertSummaryDto>> GetUnpostedByArtistIdAsync(int id) =>
        concertRepository.GetUnpostedByArtistIdAsync(id);

    public Task<IEnumerable<ConcertSummaryDto>> GetUnpostedByVenueIdAsync(int id) =>
        concertRepository.GetUnpostedByVenueIdAsync(id);
}
