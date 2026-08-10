using Concertable.B2B.Concert.Domain.ReadModels;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Domain.Lifecycle;
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
    private readonly IApplyExecutor applyExecutor;
    private readonly IAcceptExecutor acceptExecutor;
    private readonly ICheckoutDispatcher checkoutDispatcher;
    private readonly IWithdrawExecutor withdrawExecutor;
    private readonly IRejectExecutor rejectExecutor;
    private readonly ICancelApplicationExecutor cancelApplicationExecutor;
    private readonly IApplicationMapper mapper;

    public ApplicationService(
        IApplicationRepository repository,
        IApplicationValidator applicationValidator,
        IApplicationNotifier notifier,
        IOpportunityService opportunityService,
        IOpportunityRepository opportunityRepository,
        IArtistModule artistModule,
        IApplyExecutor applyExecutor,
        IAcceptExecutor acceptExecutor,
        ICheckoutDispatcher checkoutDispatcher,
        IWithdrawExecutor withdrawExecutor,
        IRejectExecutor rejectExecutor,
        ICancelApplicationExecutor cancelApplicationExecutor,
        IApplicationMapper mapper)
    {
        this.repository = repository;
        this.applicationValidator = applicationValidator;
        this.notifier = notifier;
        this.opportunityService = opportunityService;
        this.opportunityRepository = opportunityRepository;
        this.artistModule = artistModule;
        this.applyExecutor = applyExecutor;
        this.acceptExecutor = acceptExecutor;
        this.checkoutDispatcher = checkoutDispatcher;
        this.withdrawExecutor = withdrawExecutor;
        this.rejectExecutor = rejectExecutor;
        this.cancelApplicationExecutor = cancelApplicationExecutor;
        this.mapper = mapper;
    }

    public async Task<IReadOnlyList<ApplicationDto>> GetByOpportunityIdAsync(int id)
    {
        var response = await opportunityService.OwnsOpportunityAsync(id);

        if (!response)
            throw new ForbiddenException("You do not own this Concert Opportunity");

        var applications = await repository.GetByOpportunityIdAsync(id);

        return await mapper.ToDtosAsync(applications);
    }

    public async Task<IReadOnlyList<ApplicationDto>> GetPendingForArtistAsync()
    {
        var artistId = (await artistModule.GetIdForCurrentTenantAsync()).Match(
            value => value,
            () => throw new ForbiddenException("You must have an Artist account"));
        var applications = await repository.GetPendingByArtistIdAsync(artistId);
        return await mapper.ToDtosAsync(applications);
    }

    public async Task<IReadOnlyList<ApplicationDto>> GetRecentDeniedForArtistAsync()
    {
        var artistId = (await artistModule.GetIdForCurrentTenantAsync()).Match(
            value => value,
            () => throw new ForbiddenException("You must have an Artist account"));
        var applications = await repository.GetRecentDeniedByArtistIdAsync(artistId);
        return await mapper.ToDtosAsync(applications);
    }

    public Task<Result<ApplicationDto, ApplyApplicationError>> ApplyAsync(
        int opportunityId,
        ESignatureRequest eSignature) =>
        ApplyAsync(opportunityId, null, eSignature);

    public async Task<Result<ApplicationDto, ApplyApplicationError>> ApplyAsync(
        int opportunityId,
        string? paymentMethodId,
        ESignatureRequest eSignature)
    {
        var artist = await ResolveArtistIdAsync();
        if (artist.TryGetError(out var artistError))
            return Result.Failure<ApplicationDto, ApplyApplicationError>(artistError);
        artist.TryGetValue(out var artistId);

        var validation = await ValidateCanApplyAsync(opportunityId, artistId);
        if (validation.TryGetError(out var validationError))
            return Result.Failure<ApplicationDto, ApplyApplicationError>(validationError);

        var execution = await applyExecutor.ApplyAsync(opportunityId, artistId, paymentMethodId, eSignature);
        if (execution.TryGetError(out var executionError))
            return Result.Failure<ApplicationDto, ApplyApplicationError>(executionError);
        var application = execution.Match(
            value => value,
            _ => throw new InvalidOperationException("Successful application execution returned no application."));

        await notifier.AppliedAsync(application.Id);

        var saved = (await GetByIdAsync(application.Id)).Match(
            value => value,
            _ => throw new InvalidOperationException($"Application {application.Id} not found after creation."));
        return Result.Success<ApplicationDto, ApplyApplicationError>(saved);
    }

    private async Task<Result<int, ApplyApplicationError>> ResolveArtistIdAsync() =>
        (await artistModule.GetIdForCurrentTenantAsync())
            .OrFailure(() => (ApplyApplicationError)new ApplyApplicationError.MissingArtist());

    private async Task<UnitResult<ApplyApplicationError>> ValidateCanApplyAsync(int opportunityId, int artistId)
    {
        var opportunity = await opportunityRepository.GetByIdAsync(opportunityId);
        if (opportunity is null)
            return UnitResult.Failure<ApplyApplicationError>(
                new ApplyApplicationError.OpportunityNotFound(opportunityId));

        if (await repository.ExistsForOpportunityAndArtistAsync(opportunityId, artistId))
            return UnitResult.Failure<ApplyApplicationError>(
                new ApplyApplicationError.AlreadyApplied());

        var result = await applicationValidator.CanApplyAsync(opportunity, artistId);
        if (result.TryGetError(out var errors))
            return UnitResult.Failure<ApplyApplicationError>(
                new ApplyApplicationError.Invalid(errors));

        var artistGenres = await artistModule.GetGenresAsync(artistId);
        var opportunityGenres = opportunity.Genres.ToHashSet();

        if (opportunityGenres.Count > 0 && !artistGenres.Overlaps(opportunityGenres))
            return UnitResult.Failure<ApplyApplicationError>(
                new ApplyApplicationError.GenreMismatch());

        return UnitResult.Success<ApplyApplicationError>();
    }

    public async Task<Checkout> ApplyCheckoutAsync(int opportunityId)
    {
        var result = await applicationValidator.CanApplyAsync(opportunityId);
        if (result.TryGetError(out var error))
            throw new BadRequestException(error.Definition.Message);

        return await checkoutDispatcher.ApplyCheckoutAsync(opportunityId);
    }

    public Task<Checkout> AcceptCheckoutAsync(int applicationId) =>
        checkoutDispatcher.AcceptCheckoutAsync(applicationId);

    public async Task<UnitResult<AcceptApplicationError>> AcceptAsync(
        int applicationId,
        string? paymentMethodId,
        ESignatureRequest eSignature,
        CancellationToken ct = default)
    {
        var result = await applicationValidator.CanAcceptAsync(applicationId);

        if (result.TryGetError(out var error))
            return UnitResult.Failure<AcceptApplicationError>(new AcceptApplicationError.Ineligible(error));

        var acceptance = await acceptExecutor.AcceptAsync(applicationId, paymentMethodId, eSignature, ct);
        if (acceptance.TryGetError(out var acceptanceError))
            return UnitResult.Failure(acceptanceError);

        await notifier.AcceptedAsync(applicationId);
        return UnitResult.Success<AcceptApplicationError>();
    }

    public async Task<UnitResult<CancelApplicationError>> WithdrawAsync(
        int applicationId,
        CancellationToken ct = default)
    {
        var withdrawal = await withdrawExecutor.WithdrawAsync(applicationId, ct);
        if (withdrawal.TryGetError(out var withdrawalError))
            return UnitResult.Failure(withdrawalError);

        await notifier.WithdrawnAsync(applicationId);
        return UnitResult.Success<CancelApplicationError>();
    }

    public async Task<UnitResult<RejectApplicationError>> RejectAsync(int applicationId)
    {
        var result = await rejectExecutor.RejectAsync(applicationId);
        if (result.TryGetError(out var error))
        {
            RejectApplicationError rejectionError = error switch
            {
                LifecycleTransitionError.ApplicationNotFound(var missingId) =>
                    new RejectApplicationError.ApplicationNotFound(missingId),
                LifecycleTransitionError.InvalidTransition(var current, var trigger) =>
                    new RejectApplicationError.InvalidTransition(current, trigger)
            };
            return UnitResult.Failure(rejectionError);
        }

        await notifier.RejectedAsync(applicationId);
        return UnitResult.Success<RejectApplicationError>();
    }

    public async Task<UnitResult<CancelApplicationError>> CancelAsync(
        int applicationId,
        CancellationToken ct = default)
    {
        var cancellation = await cancelApplicationExecutor.CancelAsync(applicationId, ct);
        if (cancellation.TryGetError(out var cancellationError))
            return UnitResult.Failure(cancellationError);

        await notifier.CancelledAsync(applicationId);
        return UnitResult.Success<CancelApplicationError>();
    }

    public async Task<Option<(ArtistReadModel, VenueReadModel)>> GetArtistAndVenueByIdAsync(int id) =>
        (await repository.GetArtistAndVenueByIdAsync(id)).ToOption();

    public Task<Result<ApplicationDto, ApplicationError>> GetByIdAsync(int id) =>
        repository.GetByIdAsync(id)
            .ToOption()
            .OrFailure(() => (ApplicationError)new ApplicationError.NotFound(id))
            .MapAsync(mapper.ToDtoAsync);
}
