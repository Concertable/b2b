using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Application.Application.DTOs;
using Concertable.B2B.Application.Application.Errors;
using Concertable.B2B.Application.Application.Mappers;
using Concertable.B2B.Application.Contracts.Enums;
using Concertable.B2B.Authorization.Contracts;
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
    private readonly IApplicationPrivilegedRepository privilegedRepository;
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
    private readonly IPrivilegedUnitOfWorkBehavior privilegedUnitOfWork;
    private readonly IMembershipContext membership;
    private readonly IMembershipAuthorityFence authorityFence;
    private readonly IPermissionCatalog permissionCatalog;
    private readonly ICommandExecutor commandExecutor;

    public ApplicationService(
        IApplicationRepository applicationRepository,
        IApplicationPrivilegedRepository privilegedRepository,
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
        IPrivilegedUnitOfWorkBehavior privilegedUnitOfWork,
        IMembershipContext membership,
        IMembershipAuthorityFence authorityFence,
        IPermissionCatalog permissionCatalog,
        ICommandExecutor commandExecutor)
    {
        this.applicationRepository = applicationRepository;
        this.privilegedRepository = privilegedRepository;
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
        this.privilegedUnitOfWork = privilegedUnitOfWork;
        this.membership = membership;
        this.authorityFence = authorityFence;
        this.permissionCatalog = permissionCatalog;
        this.commandExecutor = commandExecutor;
    }

    public Task<Result<ApplicationDetailsDto, ApplicationError>> GetByIdAsync(int id) =>
        applicationRepository.GetByIdAsync(id)
            .ToOption()
            .OrFailure(() => (ApplicationError)new ApplicationError.NotFound(id))
            .MapAsync(async application => new ApplicationDetailsDto(
                await mapper.ToDtoAsync(application),
                application.ArtistTenantId == tenantContext.TenantId
                    ? ApplicationSide.Artist
                    : ApplicationSide.Venue));

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
        workflow.ApplyAsync(opportunityId, eSignature, ct);

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

    public async Task<UnitResult<WithdrawApplicationError>> WithdrawAsync(
        int applicationId,
        CancellationToken ct = default)
    {
        if (membership.Membership is not { } actor)
            return new WithdrawApplicationError.NotPermitted();

        try
        {
            return await commandExecutor.ExecuteAsync<ApplicationService, UnitResult<WithdrawApplicationError>>(
                (service, token) => service.WithdrawCommandAsync(applicationId, actor, token),
                ct);
        }
        catch (DbUpdateException exception)
            when (exception.IsApplicationConcurrencyConflict(applicationId))
        {
            return await commandExecutor.ExecuteAsync<ApplicationService, UnitResult<WithdrawApplicationError>>(
                (service, token) => service.ClassifyWithdrawConflictAsync(applicationId, token),
                ct);
        }
    }

    public async Task<UnitResult<RejectApplicationError>> RejectAsync(
        int applicationId,
        CancellationToken ct = default)
    {
        if (membership.Membership is not { } actor)
            return new RejectApplicationError.NotPermitted();

        try
        {
            return await commandExecutor.ExecuteAsync<ApplicationService, UnitResult<RejectApplicationError>>(
                (service, token) => service.RejectCommandAsync(applicationId, actor, token),
                ct);
        }
        catch (DbUpdateException exception)
            when (exception.IsApplicationConcurrencyConflict(applicationId))
        {
            return await commandExecutor.ExecuteAsync<ApplicationService, UnitResult<RejectApplicationError>>(
                (service, token) => service.ClassifyRejectConflictAsync(applicationId, token),
                ct);
        }
    }

    public async Task<UnitResult<CancelApplicationError>> CancelAsync(
        int applicationId,
        CancellationToken ct = default)
    {
        if (membership.Membership is not { } actor)
            return new CancelApplicationError.NotPermitted();

        try
        {
            return await commandExecutor.ExecuteAsync<ApplicationService, UnitResult<CancelApplicationError>>(
                (service, token) => service.CancelCommandAsync(applicationId, actor, token),
                ct);
        }
        catch (DbUpdateException exception)
            when (exception.IsApplicationConcurrencyConflict(applicationId))
        {
            return await commandExecutor.ExecuteAsync<ApplicationService, UnitResult<CancelApplicationError>>(
                (service, token) => service.ClassifyCancelConflictAsync(applicationId, token),
                ct);
        }
    }

    private Task<UnitResult<WithdrawApplicationError>> WithdrawCommandAsync(
        int applicationId,
        MembershipSnapshot actor,
        CancellationToken ct) =>
        privilegedUnitOfWork.ExecuteAsync(() => WithdrawCoreAsync(applicationId, actor, ct), ct);

    private Task<UnitResult<RejectApplicationError>> RejectCommandAsync(
        int applicationId,
        MembershipSnapshot actor,
        CancellationToken ct) =>
        privilegedUnitOfWork.ExecuteAsync(() => RejectCoreAsync(applicationId, actor, ct), ct);

    private Task<UnitResult<CancelApplicationError>> CancelCommandAsync(
        int applicationId,
        MembershipSnapshot actor,
        CancellationToken ct) =>
        privilegedUnitOfWork.ExecuteAsync(() => CancelCoreAsync(applicationId, actor, ct), ct);

    private async Task<UnitResult<WithdrawApplicationError>> ClassifyWithdrawConflictAsync(
        int applicationId,
        CancellationToken ct)
    {
        if (await privilegedRepository.GetStateByIdAsync(applicationId, ct) == ApplicationState.Withdrawn)
            return new Success();

        return new WithdrawApplicationError.Superseded(applicationId);
    }

    private async Task<UnitResult<WithdrawApplicationError>> WithdrawCoreAsync(
        int applicationId,
        MembershipSnapshot expectedActor,
        CancellationToken ct)
    {
        var actor = await authorityFence.RequireCurrentAsync(expectedActor, ct);
        if (actor is null
            || !permissionCatalog.Grants(actor.Role, TenantPermission.ApplicationsSubmit))
            return new WithdrawApplicationError.NotPermitted();

        var application = await privilegedRepository.GetByIdForUpdateAsync(applicationId, ct);
        if (application is null)
            return new WithdrawApplicationError.ApplicationNotFound(applicationId);
        if (application.ArtistTenantId != actor.TenantId
            || !ResourceGrantPolicy.Allows(
                application.AccessGrants,
                ApplicationAccessScope.Proposal,
                actor,
                permissionCatalog.AudienceFor(actor.Role, TenantPermission.ApplicationsSubmit),
                timeProvider.GetUtcNow().UtcDateTime))
            return new WithdrawApplicationError.NotPermitted();
        if (application.Withdraw().TryGetError(out var transitionError))
            return new WithdrawApplicationError.InvalidTransition(transitionError);
        application.NotifyCounterparty(ApplicationNotification.Withdrawn);
        await notifier.WithdrawnAsync(applicationId);
        return new Success();
    }

    private async Task<UnitResult<RejectApplicationError>> ClassifyRejectConflictAsync(
        int applicationId,
        CancellationToken ct)
    {
        if (await privilegedRepository.GetStateByIdAsync(applicationId, ct) == ApplicationState.Rejected)
            return new Success();

        return new RejectApplicationError.Superseded(applicationId);
    }

    private async Task<UnitResult<RejectApplicationError>> RejectCoreAsync(
        int applicationId,
        MembershipSnapshot expectedActor,
        CancellationToken ct)
    {
        var actor = await authorityFence.RequireCurrentAsync(expectedActor, ct);
        if (actor is null
            || !permissionCatalog.Grants(actor.Role, TenantPermission.ApplicationsDecide))
            return new RejectApplicationError.NotPermitted();

        var application = await privilegedRepository.GetByIdForUpdateAsync(applicationId, ct);
        if (application is null)
            return new RejectApplicationError.ApplicationNotFound(applicationId);
        if (application.VenueTenantId != actor.TenantId
            || !ResourceGrantPolicy.Allows(
                application.AccessGrants,
                ApplicationAccessScope.Proposal,
                actor,
                permissionCatalog.AudienceFor(actor.Role, TenantPermission.ApplicationsDecide),
                timeProvider.GetUtcNow().UtcDateTime))
            return new RejectApplicationError.NotPermitted();
        if (application.Reject().TryGetError(out var transitionError))
            return new RejectApplicationError.InvalidTransition(transitionError);
        application.NotifyCounterparty(ApplicationNotification.Rejected);
        await notifier.RejectedAsync(applicationId);
        return new Success();
    }

    private async Task<UnitResult<CancelApplicationError>> ClassifyCancelConflictAsync(
        int applicationId,
        CancellationToken ct)
    {
        if (await privilegedRepository.GetStateByIdAsync(applicationId, ct) == ApplicationState.Cancelled)
            return new Success();

        return new CancelApplicationError.Superseded(applicationId);
    }

    private async Task<UnitResult<CancelApplicationError>> CancelCoreAsync(
        int applicationId,
        MembershipSnapshot expectedActor,
        CancellationToken ct)
    {
        var actor = await authorityFence.RequireCurrentAsync(expectedActor, ct);
        if (actor is null
            || !permissionCatalog.Grants(actor.Role, TenantPermission.ApplicationsDecide))
            return new CancelApplicationError.NotPermitted();

        var application = await privilegedRepository.GetByIdForUpdateAsync(applicationId, ct);
        if (application is null)
            return new CancelApplicationError.ApplicationNotFound(applicationId);
        if (application.VenueTenantId != actor.TenantId
            || !ResourceGrantPolicy.Allows(
                application.AccessGrants,
                ApplicationAccessScope.Proposal,
                actor,
                permissionCatalog.AudienceFor(actor.Role, TenantPermission.ApplicationsDecide),
                timeProvider.GetUtcNow().UtcDateTime))
            return new CancelApplicationError.NotPermitted();
        if (application.Cancel().TryGetError(out var transitionError))
            return new CancelApplicationError.InvalidTransition(transitionError);
        application.NotifyCounterparty(ApplicationNotification.ApplicationCancelled);
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
