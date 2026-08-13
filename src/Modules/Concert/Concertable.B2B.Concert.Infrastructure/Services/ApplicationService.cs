using Concertable.B2B.Concert.Domain.ReadModels;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Domain.Lifecycle;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class ApplicationService : IApplicationService
{
    private readonly IApplicationRepository repository;
    private readonly IApplicationValidator applicationValidator;
    private readonly IApplicationNotifier notifier;
    private readonly IOpportunityService opportunityService;
    private readonly IOpportunityRepository opportunityRepository;
    private readonly IArtistModule artistModule;
    private readonly IApplicationExecutor executor;
    private readonly ICheckoutDispatcher checkoutDispatcher;
    private readonly IApplicationMapper mapper;

    public ApplicationService(
        IApplicationRepository repository,
        IApplicationValidator applicationValidator,
        IApplicationNotifier notifier,
        IOpportunityService opportunityService,
        IOpportunityRepository opportunityRepository,
        IArtistModule artistModule,
        IApplicationExecutor executor,
        ICheckoutDispatcher checkoutDispatcher,
        IApplicationMapper mapper)
    {
        this.repository = repository;
        this.applicationValidator = applicationValidator;
        this.notifier = notifier;
        this.opportunityService = opportunityService;
        this.opportunityRepository = opportunityRepository;
        this.artistModule = artistModule;
        this.executor = executor;
        this.checkoutDispatcher = checkoutDispatcher;
        this.mapper = mapper;
    }

    public async Task<Option<FinancialOperation>> GetFinancialOperationAsync(
        int applicationId,
        CancellationToken ct = default) =>
        await repository.GetFinancialOperationAsync(applicationId, ct);

    public async Task<Result<IReadOnlyList<ApplicationDto>, ApplicationError>> GetByOpportunityIdAsync(int id)
    {
        var response = await opportunityService.OwnsOpportunityAsync(id);

        if (!response)
            return new ApplicationError.OpportunityForbidden(id);

        var applications = await repository.GetByOpportunityIdAsync(id);

        return new Success<IReadOnlyList<ApplicationDto>>(
            await mapper.ToDtosAsync(applications));
    }

    public async Task<Result<IReadOnlyList<ApplicationDto>, ApplicationError>> GetPendingForArtistAsync()
    {
        var artist = await artistModule.GetIdForCurrentTenantAsync();
        if (!artist.TryGetValue(out var artistId))
            return new ApplicationError.MissingArtist();

        var applications = await repository.GetPendingByArtistIdAsync(artistId);
        return new Success<IReadOnlyList<ApplicationDto>>(
            await mapper.ToDtosAsync(applications));
    }

    public async Task<Result<IReadOnlyList<ApplicationDto>, ApplicationError>> GetRecentDeniedForArtistAsync()
    {
        var artist = await artistModule.GetIdForCurrentTenantAsync();
        if (!artist.TryGetValue(out var artistId))
            return new ApplicationError.MissingArtist();

        var applications = await repository.GetRecentDeniedByArtistIdAsync(artistId);
        return new Success<IReadOnlyList<ApplicationDto>>(
            await mapper.ToDtosAsync(applications));
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
            return artistError;
        artist.TryGetValue(out var artistId);

        var validation = await ValidateCanApplyAsync(opportunityId, artistId);
        if (validation.TryGetError(out var validationError))
            return validationError;

        var execution = await executor.ApplyAsync(opportunityId, artistId, paymentMethodId, eSignature);
        if (execution.TryGetError(out var executionError))
            return executionError;
        var application = execution.Match(
            value => value,
            _ => throw new InvalidOperationException("Successful application execution returned no application."));

        await notifier.AppliedAsync(application.Id);

        var saved = (await GetByIdAsync(application.Id)).Match(
            value => value,
            _ => throw new InvalidOperationException($"Application {application.Id} not found after creation."));
        return saved;
    }

    private async Task<Result<int, ApplyApplicationError>> ResolveArtistIdAsync() =>
        (await artistModule.GetIdForCurrentTenantAsync())
            .OrFailure(() => (ApplyApplicationError)new ApplyApplicationError.MissingArtist());

    private async Task<UnitResult<ApplyApplicationError>> ValidateCanApplyAsync(int opportunityId, int artistId)
    {
        var opportunity = await opportunityRepository.GetByIdAsync(opportunityId);
        if (opportunity is null)
            return new ApplyApplicationError.OpportunityNotFound(opportunityId);

        if (await repository.ExistsForOpportunityAndArtistAsync(opportunityId, artistId))
            return new ApplyApplicationError.AlreadyApplied();

        var result = await applicationValidator.CanApplyAsync(opportunity, artistId);
        if (result.TryGetErrors(out var errors))
            return new ApplyApplicationError.Invalid(new ValidationErrors(errors.ToDictionary()));

        var artistGenres = await artistModule.GetGenresAsync(artistId);
        var opportunityGenres = opportunity.Genres.ToHashSet();

        if (opportunityGenres.Count > 0 && !artistGenres.Overlaps(opportunityGenres))
            return new ApplyApplicationError.GenreMismatch();

        return new Success();
    }

    public async Task<bool> CanApplyAsync(int opportunityId) =>
        (await CheckCanApplyAsync(opportunityId)).IsSuccess;

    public async Task<bool> CanAcceptAsync(int applicationId) =>
        (await CheckCanAcceptAsync(applicationId)).IsSuccess;

    public async Task<Result<Checkout, ApplicationEligibilityError>> ApplyCheckoutAsync(int opportunityId)
    {
        var result = await CheckCanApplyAsync(opportunityId);
        if (result.TryGetError(out var error))
            return error;

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
        var result = await CheckCanAcceptAsync(applicationId);

        if (result.TryGetError(out var error))
            return new AcceptApplicationError.Ineligible(error);

        var acceptance = await executor.AcceptAsync(applicationId, paymentMethodId, eSignature, ct);
        if (acceptance.TryGetError(out var acceptanceError))
            return acceptanceError;

        await notifier.AcceptedAsync(applicationId);
        return new Success();
    }

    private async Task<UnitResult<ApplicationEligibilityError>> CheckCanApplyAsync(int opportunityId)
    {
        var artistId = await artistModule.GetIdForCurrentTenantAsync();
        if (!artistId.TryGetValue(out var value))
            return new ApplicationEligibilityError.MissingArtist();

        var opportunity = await opportunityRepository.GetByIdAsync(opportunityId);
        if (opportunity is null)
            return new ApplicationEligibilityError.OpportunityNotFound();

        var validation = await applicationValidator.CanApplyAsync(opportunity, value);
        if (validation.TryGetErrors(out var errors))
            return new ApplicationEligibilityError.Invalid(new ValidationErrors(errors.ToDictionary()));

        return new Success();
    }

    private async Task<UnitResult<ApplicationEligibilityError>> CheckCanAcceptAsync(int applicationId)
    {
        var opportunity = await opportunityRepository.GetByApplicationIdAsync(applicationId);
        if (opportunity is null)
            return new ApplicationEligibilityError.OpportunityNotFound();

        var application = await repository.GetByIdAsync(applicationId);
        if (application is null)
            return new ApplicationEligibilityError.ApplicationNotFound();

        var validation = await applicationValidator.CanAcceptAsync(opportunity, application);
        if (validation.TryGetErrors(out var errors))
            return new ApplicationEligibilityError.Invalid(new ValidationErrors(errors.ToDictionary()));

        return new Success();
    }

    public async Task<UnitResult<CancelApplicationError>> WithdrawAsync(
        int applicationId,
        CancellationToken ct = default)
    {
        var withdrawal = await executor.WithdrawAsync(applicationId, ct);
        if (withdrawal.TryGetError(out var withdrawalError))
            return withdrawalError;

        await notifier.WithdrawnAsync(applicationId);
        return new Success();
    }

    public async Task<UnitResult<RejectApplicationError>> RejectAsync(int applicationId)
    {
        var result = await executor.RejectAsync(applicationId);
        if (result.TryGetError(out var error))
        {
            RejectApplicationError rejectionError = error switch
            {
                LifecycleTransitionError.ApplicationNotFound(var missingId) =>
                    new RejectApplicationError.ApplicationNotFound(missingId),
                LifecycleTransitionError.InvalidTransition(var current, var trigger) =>
                    new RejectApplicationError.InvalidTransition(current, trigger)
            };
            return rejectionError;
        }

        await notifier.RejectedAsync(applicationId);
        return new Success();
    }

    public async Task<UnitResult<CancelApplicationError>> CancelAsync(
        int applicationId,
        CancellationToken ct = default)
    {
        var cancellation = await executor.CancelAsync(applicationId, ct);
        if (cancellation.TryGetError(out var cancellationError))
            return cancellationError;

        await notifier.CancelledAsync(applicationId);
        return new Success();
    }

    public async Task<Option<(ArtistReadModel, VenueReadModel)>> GetArtistAndVenueByIdAsync(int id) =>
        (await repository.GetArtistAndVenueByIdAsync(id)).ToOption();

    public Task<Result<ApplicationDto, ApplicationError>> GetByIdAsync(int id) =>
        repository.GetByIdAsync(id)
            .ToOption()
            .OrFailure(() => (ApplicationError)new ApplicationError.NotFound(id))
            .MapAsync(mapper.ToDtoAsync);
}
