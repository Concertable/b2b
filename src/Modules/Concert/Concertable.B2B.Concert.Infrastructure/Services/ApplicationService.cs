using Concertable.B2B.Concert.Domain.ReadModels;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class ApplicationService : IApplicationService
{
    private readonly IApplicationRepository repository;
    private readonly IApplicationValidator applicationValidator;
    private readonly IApplicationNotifier notifier;
    private readonly IOpportunityService opportunityService;
    private readonly IOpportunityRepository opportunityRepository;
    private readonly IArtistModule artistModule;
    private readonly IApplyDispatcher applyDispatcher;
    private readonly IAcceptanceDispatcher acceptanceDispatcher;
    private readonly ICheckoutDispatcher checkoutDispatcher;
    private readonly IWithdrawalDispatcher withdrawalDispatcher;
    private readonly IRejectionDispatcher rejectionDispatcher;
    private readonly IApplicationMapper mapper;

    public ApplicationService(
        IApplicationRepository repository,
        IApplicationValidator applicationValidator,
        IApplicationNotifier notifier,
        IOpportunityService opportunityService,
        IOpportunityRepository opportunityRepository,
        IArtistModule artistModule,
        IApplyDispatcher applyDispatcher,
        IAcceptanceDispatcher acceptanceDispatcher,
        ICheckoutDispatcher checkoutDispatcher,
        IWithdrawalDispatcher withdrawalDispatcher,
        IRejectionDispatcher rejectionDispatcher,
        IApplicationMapper mapper)
    {
        this.repository = repository;
        this.applicationValidator = applicationValidator;
        this.notifier = notifier;
        this.opportunityService = opportunityService;
        this.opportunityRepository = opportunityRepository;
        this.artistModule = artistModule;
        this.applyDispatcher = applyDispatcher;
        this.acceptanceDispatcher = acceptanceDispatcher;
        this.checkoutDispatcher = checkoutDispatcher;
        this.withdrawalDispatcher = withdrawalDispatcher;
        this.rejectionDispatcher = rejectionDispatcher;
        this.mapper = mapper;
    }

    public async Task<IEnumerable<ApplicationDto>> GetByOpportunityIdAsync(int id)
    {
        var response = await opportunityService.OwnsOpportunityAsync(id);

        if (!response)
            throw new ForbiddenException("You do not own this Concert Opportunity");

        var applications = await repository.GetByOpportunityIdAsync(id);

        return await mapper.ToDtosAsync(applications);
    }

    public async Task<IEnumerable<ApplicationDto>> GetPendingForArtistAsync()
    {
        var artistId = await artistModule.GetIdForCurrentTenantAsync()
            ?? throw new ForbiddenException("You must have an Artist account");
        var applications = await repository.GetPendingByArtistIdAsync(artistId);
        return await mapper.ToDtosAsync(applications);
    }

    public async Task<IEnumerable<ApplicationDto>> GetRecentDeniedForArtistAsync()
    {
        var artistId = await artistModule.GetIdForCurrentTenantAsync()
            ?? throw new ForbiddenException("You must have an Artist account");
        var applications = await repository.GetRecentDeniedByArtistIdAsync(artistId);
        return await mapper.ToDtosAsync(applications);
    }

    public async Task<ApplicationDto> ApplyAsync(int opportunityId)
    {
        var artistId = await ResolveArtistIdAsync();
        await ValidateCanApplyAsync(opportunityId, artistId);

        var application = await applyDispatcher.ApplyAsync(opportunityId, artistId);
        await notifier.AppliedAsync(application.Id);

        return await GetByIdAsync(application.Id);
    }

    public async Task<ApplicationDto> ApplyAsync(int opportunityId, string paymentMethodId)
    {
        var artistId = await ResolveArtistIdAsync();
        await ValidateCanApplyAsync(opportunityId, artistId);

        var application = await applyDispatcher.ApplyAsync(opportunityId, artistId, paymentMethodId);
        await notifier.AppliedAsync(application.Id);

        return await GetByIdAsync(application.Id);
    }

    private async Task<int> ResolveArtistIdAsync() =>
        await artistModule.GetIdForCurrentTenantAsync()
            ?? throw new ForbiddenException("You must create an Artist account before you apply for a concert opportunity");

    private async Task ValidateCanApplyAsync(int opportunityId, int artistId)
    {
        var opportunity = await opportunityRepository.GetByIdAsync(opportunityId)
            ?? throw new NotFoundException("Concert Opportunity not found");

        var result = await applicationValidator.CanApplyAsync(opportunity, artistId);
        if (result.IsFailed)
            throw new BadRequestException(result.Errors);

        var artistGenres = await artistModule.GetGenresAsync(artistId);
        var opportunityGenres = opportunity.Genres.ToHashSet();

        if (opportunityGenres.Count > 0 && !artistGenres.Overlaps(opportunityGenres))
            throw new BadRequestException("You need to have the same genres as the Concert Opportunity to be able to apply to it");
    }

    public async Task<Checkout> ApplyCheckoutAsync(int opportunityId)
    {
        var result = await applicationValidator.CanApplyAsync(opportunityId);
        if (result.IsFailed)
            throw new BadRequestException(result.Errors);

        return await checkoutDispatcher.ApplyCheckoutAsync(opportunityId);
    }

    public Task<Checkout> AcceptCheckoutAsync(int applicationId) =>
        checkoutDispatcher.AcceptCheckoutAsync(applicationId);

    public async Task AcceptAsync(int applicationId, string? paymentMethodId)
    {
        var result = await applicationValidator.CanAcceptAsync(applicationId);

        if (result.IsFailed)
            throw new BadRequestException(result.Errors);

        await acceptanceDispatcher.AcceptAsync(applicationId, paymentMethodId);
        await notifier.AcceptedAsync(applicationId);
    }

    public async Task WithdrawAsync(int applicationId)
    {
        await withdrawalDispatcher.WithdrawAsync(applicationId);
        await notifier.WithdrawnAsync(applicationId);
    }

    public async Task RejectAsync(int applicationId)
    {
        await rejectionDispatcher.RejectAsync(applicationId);
        await notifier.RejectedAsync(applicationId);
    }

    public async Task<(ArtistReadModel, VenueReadModel)?> GetArtistAndVenueByIdAsync(int id) =>
        await repository.GetArtistAndVenueByIdAsync(id);

    public async Task<ApplicationDto> GetByIdAsync(int id)
    {
        var application = await repository.GetByIdAsync(id)
            ?? throw new NotFoundException("Application not found");
        return await mapper.ToDtoAsync(application);
    }
}
