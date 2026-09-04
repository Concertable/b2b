using Concertable.B2B.Application.Application.Errors;
using Concertable.B2B.Application.Application.Mappers;
using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Application.Domain.Events;
using Concertable.B2B.Application.Domain.Lifecycle;
using Concertable.B2B.Application.Infrastructure.Extensions;
using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Opportunity.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Application.Infrastructure.Services;

internal sealed class ApplicationService : IApplicationService
{
    private readonly IApplicationRepository applicationRepository;
    private readonly IApplicationValidator validator;
    private readonly IApplicationNotifier notifier;
    private readonly IApplicationWorkflow workflow;
    private readonly IApplicationEligibility eligibility;
    private readonly IArtistModule artistModule;
    private readonly IOpportunityModule opportunityModule;
    private readonly ITenantContext tenantContext;
    private readonly IApplicationCheckoutService checkoutService;
    private readonly IApplicationMapper mapper;
    private readonly TimeProvider timeProvider;
    private readonly IUnitOfWork unitOfWork;
    private readonly IUnitOfWorkBehavior unitOfWorkBehavior;

    public ApplicationService(
        IApplicationRepository applicationRepository,
        IApplicationValidator validator,
        IApplicationNotifier notifier,
        IApplicationWorkflow workflow,
        IApplicationEligibility eligibility,
        IArtistModule artistModule,
        IOpportunityModule opportunityModule,
        ITenantContext tenantContext,
        IApplicationCheckoutService checkoutService,
        IApplicationMapper mapper,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork,
        IUnitOfWorkBehavior unitOfWorkBehavior)
    {
        this.applicationRepository = applicationRepository;
        this.validator = validator;
        this.notifier = notifier;
        this.workflow = workflow;
        this.eligibility = eligibility;
        this.artistModule = artistModule;
        this.opportunityModule = opportunityModule;
        this.tenantContext = tenantContext;
        this.checkoutService = checkoutService;
        this.mapper = mapper;
        this.timeProvider = timeProvider;
        this.unitOfWork = unitOfWork;
        this.unitOfWorkBehavior = unitOfWorkBehavior;
    }

    public Task<Result<ApplicationDto, ApplicationError>> GetByIdAsync(int id) =>
        applicationRepository.GetByIdAsync(id)
            .ToOption()
            .OrFailure(() => (ApplicationError)new ApplicationError.NotFound(id))
            .MapAsync(application => mapper.ToDtoAsync(application));

    public async Task<Result<IReadOnlyList<ApplicationDto>, ApplicationError>> GetByOpportunityIdAsync(int id)
    {
        var opportunityOption = await opportunityModule.GetAsync(id);
        if (!opportunityOption.TryGetValue(out var opportunity) ||
            opportunity.VenueTenantId != tenantContext.TenantId)
            return new ApplicationError.OpportunityForbidden(id);

        var applications = await applicationRepository.GetByOpportunityIdAsync(id);
        return new Success<IReadOnlyList<ApplicationDto>>(await mapper.ToDtosAsync(applications));
    }

    public async Task<Result<IReadOnlyList<ApplicationDto>, ApplicationError>> GetPendingForArtistAsync()
    {
        var artistOption = await artistModule.GetCurrentProfileAsync();
        if (!artistOption.TryGetValue(out var artist))
            return new ApplicationError.MissingArtist();

        var applications = await applicationRepository.GetByArtistTenantIdAndStateAsync(
            artist.TenantId,
            ApplicationState.Applied);
        var dtos = await mapper.ToDtosAsync(applications);
        return new Success<IReadOnlyList<ApplicationDto>>(
            dtos.Where(application => application.Opportunity.StartDate > timeProvider.GetUtcNow())
                .ToList());
    }

    public async Task<Result<IReadOnlyList<ApplicationDto>, ApplicationError>> GetRecentDeniedForArtistAsync()
    {
        var artistOption = await artistModule.GetCurrentProfileAsync();
        if (!artistOption.TryGetValue(out var artist))
            return new ApplicationError.MissingArtist();

        var applications = await applicationRepository.GetByArtistTenantIdAndStateAsync(
            artist.TenantId,
            ApplicationState.Rejected);
        var dtos = await mapper.ToDtosAsync(applications);
        return new Success<IReadOnlyList<ApplicationDto>>(
            dtos.OrderByDescending(application => application.Opportunity.EndDate)
                .Take(5)
                .ToList());
    }

    public async Task<Result<IReadOnlyList<ApplicationDto>, ApplicationError>> GetPendingForCurrentVenueAsync()
    {
        if (tenantContext.TenantId is not { } tenantId)
            return new ApplicationError.MissingVenue();

        var applications = await applicationRepository.GetByVenueTenantIdAndStateAsync(
            tenantId,
            ApplicationState.Applied);
        var now = timeProvider.GetUtcNow();
        var dtos = await mapper.ToDtosAsync(applications);
        return new Success<IReadOnlyList<ApplicationDto>>(
            dtos.Where(application => application.Opportunity.EndDate > now)
                .OrderBy(application => application.Opportunity.StartDate)
                .ThenBy(application => application.Id)
                .Take(5)
                .ToList());
    }

    public async Task<Result<IReadOnlyList<ApplicationDto>, ApplicationError>> GetCurrentForCurrentArtistAsync()
    {
        if (tenantContext.TenantId is not { } tenantId)
            return new ApplicationError.MissingArtist();

        var applications = await applicationRepository.GetCurrentByArtistTenantIdAsync(tenantId);
        var now = timeProvider.GetUtcNow();
        var dtos = await mapper.ToDtosAsync(applications);
        return new Success<IReadOnlyList<ApplicationDto>>(
            dtos.Where(application => application.Opportunity.EndDate > now)
                .OrderBy(application => application.Opportunity.StartDate)
                .ThenBy(application => application.Id)
                .Take(10)
                .ToList());
    }

    public Task<Result<ApplicationDto, ApplyApplicationError>> ApplyAsync(
        int opportunityId,
        ESignatureRequest eSignature,
        CancellationToken ct = default) =>
        workflow.ApplyAsync(opportunityId, null, eSignature, ct);

    public Task<Result<ApplicationDto, ApplyApplicationError>> ApplyAsync(
        int opportunityId,
        string? paymentMethodId,
        ESignatureRequest eSignature,
        CancellationToken ct = default) =>
        workflow.ApplyAsync(opportunityId, paymentMethodId, eSignature, ct);

    public async Task<bool> CanApplyAsync(int opportunityId) =>
        (await CheckCanApplyAsync(opportunityId)).IsSuccess;

    public async Task<bool> CanAcceptAsync(int applicationId) =>
        (await CheckCanAcceptAsync(applicationId)).IsSuccess;

    public async Task<Result<Checkout, ApplicationCheckoutError>> ApplyCheckoutAsync(int opportunityId)
    {
        var eligibility = await CheckCanApplyAsync(opportunityId);
        if (eligibility.TryGetError(out var error))
            return new ApplicationCheckoutError.Ineligible(error);

        return await checkoutService.CreateApplyCheckoutAsync(opportunityId);
    }

    public async Task<Result<Checkout, ApplicationCheckoutError>> AcceptCheckoutAsync(int applicationId)
    {
        var eligibility = await CheckCanAcceptAsync(applicationId);
        if (eligibility.TryGetError(out var error))
            return new ApplicationCheckoutError.Ineligible(error);

        return await checkoutService.CreateAcceptCheckoutAsync(applicationId);
    }

    public Task<UnitResult<AcceptApplicationError>> AcceptAsync(
        int applicationId,
        ESignatureRequest eSignature,
        CancellationToken ct = default) =>
        workflow.AcceptAsync(applicationId, eSignature, ct);

    public Task<UnitResult<WithdrawApplicationError>> WithdrawAsync(
        int applicationId,
        CancellationToken ct = default) =>
        unitOfWorkBehavior.TryExecuteAsync(
            () => WithdrawCoreAsync(applicationId, ct),
            exception => exception.IsApplicationConcurrencyConflict(applicationId),
            _ => ClassifyWithdrawConflictAsync(applicationId, ct),
            ct);

    public Task<UnitResult<RejectApplicationError>> RejectAsync(
        int applicationId,
        CancellationToken ct = default) =>
        unitOfWorkBehavior.TryExecuteAsync(
            () => RejectCoreAsync(applicationId, ct),
            exception => exception.IsApplicationConcurrencyConflict(applicationId),
            _ => ClassifyRejectConflictAsync(applicationId, ct),
            ct);

    public Task<UnitResult<CancelApplicationError>> CancelAsync(
        int applicationId,
        CancellationToken ct = default) =>
        unitOfWorkBehavior.TryExecuteAsync(
            () => CancelCoreAsync(applicationId, ct),
            exception => exception.IsApplicationConcurrencyConflict(applicationId),
            _ => ClassifyCancelConflictAsync(applicationId, ct),
            ct);

    private async Task<UnitResult<WithdrawApplicationError>> ClassifyWithdrawConflictAsync(
        int applicationId,
        CancellationToken ct)
    {
        if (await applicationRepository.GetStateByIdAsync(applicationId, ct) == ApplicationState.Withdrawn)
            return new Success();

        return new WithdrawApplicationError.Superseded(applicationId);
    }

    private async Task<UnitResult<WithdrawApplicationError>> WithdrawCoreAsync(
        int applicationId,
        CancellationToken ct)
    {
        var application = await applicationRepository.GetByIdAsync(applicationId, ct);
        if (application is null)
            return new WithdrawApplicationError.ApplicationNotFound(applicationId);
        if (application.Withdraw().TryGetError(out var transitionError))
            return new WithdrawApplicationError.InvalidTransition(transitionError);
        application.NotifyCounterparty(ApplicationNotification.Withdrawn);
        await unitOfWork.SaveChangesAsync(ct);
        await notifier.WithdrawnAsync(applicationId);
        return new Success();
    }

    private async Task<UnitResult<RejectApplicationError>> ClassifyRejectConflictAsync(
        int applicationId,
        CancellationToken ct)
    {
        if (await applicationRepository.GetStateByIdAsync(applicationId, ct) == ApplicationState.Rejected)
            return new Success();

        return new RejectApplicationError.Superseded(applicationId);
    }

    private async Task<UnitResult<RejectApplicationError>> RejectCoreAsync(
        int applicationId,
        CancellationToken ct)
    {
        var application = await applicationRepository.GetByIdAsync(applicationId, ct);
        if (application is null)
            return new RejectApplicationError.ApplicationNotFound(applicationId);
        if (application.Reject().TryGetError(out var transitionError))
            return new RejectApplicationError.InvalidTransition(transitionError);
        application.NotifyCounterparty(ApplicationNotification.Rejected);
        await unitOfWork.SaveChangesAsync(ct);
        await notifier.RejectedAsync(applicationId);
        return new Success();
    }

    private async Task<UnitResult<CancelApplicationError>> ClassifyCancelConflictAsync(
        int applicationId,
        CancellationToken ct)
    {
        if (await applicationRepository.GetStateByIdAsync(applicationId, ct) == ApplicationState.Cancelled)
            return new Success();

        return new CancelApplicationError.Superseded(applicationId);
    }

    private async Task<UnitResult<CancelApplicationError>> CancelCoreAsync(
        int applicationId,
        CancellationToken ct)
    {
        var application = await applicationRepository.GetByIdAsync(applicationId, ct);
        if (application is null)
            return new CancelApplicationError.ApplicationNotFound(applicationId);
        if (application.Cancel().TryGetError(out var transitionError))
            return new CancelApplicationError.InvalidTransition(transitionError);
        application.NotifyCounterparty(ApplicationNotification.ApplicationCancelled);
        await unitOfWork.SaveChangesAsync(ct);
        await notifier.CancelledAsync(applicationId);
        return new Success();
    }

    private async Task<UnitResult<ApplicationEligibilityError>> CheckCanApplyAsync(int opportunityId)
    {
        var artistOption = await artistModule.GetCurrentProfileAsync();
        if (!artistOption.TryGetValue(out var artist))
            return new ApplicationEligibilityError.MissingArtist();

        var opportunityOption = await opportunityModule.GetOpenAsync(opportunityId);
        if (!opportunityOption.TryGetValue(out var opportunity))
            return new ApplicationEligibilityError.OpportunityNotFound();

        var validation = await validator.CanApplyAsync(opportunity, artist.Id);
        return validation.TryGetErrors(out var errors)
            ? new ApplicationEligibilityError.Invalid(new ValidationErrors(errors.ToDictionary()))
            : new Success();
    }

    private async Task<UnitResult<ApplicationEligibilityError>> CheckCanAcceptAsync(int applicationId)
    {
        var application = await applicationRepository.GetByIdAsync(applicationId);
        if (application is null)
            return new ApplicationEligibilityError.ApplicationNotFound();

        return await CheckCanAcceptAsync(application);
    }

    private async Task<UnitResult<ApplicationEligibilityError>> CheckCanAcceptAsync(
        ApplicationEntity application,
        CancellationToken ct = default)
    {
        var result = await eligibility.CanAcceptAsync(application, ct);
        return result.TryGetError(out var error) ? error : new Success();
    }
}
