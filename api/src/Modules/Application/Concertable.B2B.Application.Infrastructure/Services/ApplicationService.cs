using Concertable.B2B.Application.Application.Interfaces;
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
using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Opportunity.Contracts;
using Concertable.B2B.Tenant.Contracts;
using ITenantResolver = Concertable.B2B.Tenant.Contracts.ITenantResolver;
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
    private readonly IBookingModule bookingModule;
    private readonly ITenantContext tenantContext;
    private readonly IApplicationCheckoutService checkoutService;
    private readonly IApplicationResolver resolver;
    private readonly TimeProvider timeProvider;
    private readonly IPrivilegedUnitOfWorkBehavior privilegedUnitOfWork;
    private readonly IMembershipContext membership;
    private readonly IAuthorizationContext authorizationContext;
    private readonly ITenantResolver tenantResolver;
    private readonly IResourceAuthorization resources;
    private readonly IPermissionAuthorization permissions;
    private readonly ITransactionRunner transactionRunner;

    public ApplicationService(
        IApplicationRepository applicationRepository,
        IApplicationPrivilegedRepository privilegedRepository,
        IApplicationValidator validator,
        IApplicationNotifier notifier,
        IApplicationWorkflow workflow,
        IApplicationEligibility eligibility,
        IArtistModule artistModule,
        IOpportunityModule opportunityModule,
        IBookingModule bookingModule,
        ITenantContext tenantContext,
        IApplicationCheckoutService checkoutService,
        IApplicationResolver resolver,
        TimeProvider timeProvider,
        IPrivilegedUnitOfWorkBehavior privilegedUnitOfWork,
        IMembershipContext membership,
        IAuthorizationContext authorizationContext,
        ITenantResolver tenantResolver,
        IResourceAuthorization resources,
        IPermissionAuthorization permissions,
        ITransactionRunner transactionRunner)
    {
        this.applicationRepository = applicationRepository;
        this.privilegedRepository = privilegedRepository;
        this.validator = validator;
        this.notifier = notifier;
        this.workflow = workflow;
        this.eligibility = eligibility;
        this.artistModule = artistModule;
        this.opportunityModule = opportunityModule;
        this.bookingModule = bookingModule;
        this.tenantContext = tenantContext;
        this.checkoutService = checkoutService;
        this.resolver = resolver;
        this.timeProvider = timeProvider;
        this.privilegedUnitOfWork = privilegedUnitOfWork;
        this.membership = membership;
        this.authorizationContext = authorizationContext;
        this.tenantResolver = tenantResolver;
        this.resources = resources;
        this.permissions = permissions;
        this.transactionRunner = transactionRunner;
    }

    public Task<Result<ApplicationSummary, ApplicationError>> GetSummaryAsync(
        int id,
        CancellationToken ct = default) =>
        applicationRepository.GetSummaryByIdAsync(id, ct)
            .ToOption()
            .OrFailure(() => (ApplicationError)new ApplicationError.NotFound(id))
            .MapAsync(application => resolver.ResolveSummaryAsync(application, ct))
            .MapAsync(summary => AddBookingStatusAsync(summary, ct));

    public Task<Result<ApplicationProposal, ApplicationError>> GetProposalAsync(
        int id,
        CancellationToken ct = default) =>
        applicationRepository.GetProposalByIdAsync(id, ct)
            .ToOption()
            .OrFailure(() => (ApplicationError)new ApplicationError.NotFound(id))
            .MapAsync(application => resolver.ResolveProposalAsync(application, ct))
            .MapAsync(proposal => AddBookingStatusAsync(proposal, ct));

    public async Task<Result<IReadOnlyList<ApplicationProposal>, ApplicationError>> GetByOpportunityIdAsync(
        int id,
        CancellationToken ct = default)
    {
        var opportunityOption = await opportunityModule.GetAsync(id, ct);
        if (!opportunityOption.TryGetValue(out var opportunity) ||
            opportunity.VenueTenantId != tenantContext.TenantId)
            return new ApplicationError.OpportunityForbidden(id);

        var applications = await applicationRepository.GetByOpportunityIdAsync(id, ct);
        var proposals = await resolver.ResolveProposalsAsync(applications, ct);
        return new Success<IReadOnlyList<ApplicationProposal>>(await AddBookingStatusesAsync(proposals, ct));
    }

    public async Task<Result<IReadOnlyList<ApplicationProposal>, ApplicationError>> GetPendingForArtistAsync(
        CancellationToken ct = default)
    {
        var artistOption = await artistModule.GetCurrentProfileAsync(ct);
        if (!artistOption.TryGetValue(out var artist))
            return new ApplicationError.MissingArtist();

        var applications = await applicationRepository.GetByArtistTenantIdAndStateAsync(
            artist.TenantId,
            ApplicationState.Applied,
            ct);
        var dtos = await resolver.ResolveProposalsAsync(applications, ct);
        var proposals = dtos.Where(application => application.Opportunity.StartDate > timeProvider.GetUtcNow())
            .ToList();
        return new Success<IReadOnlyList<ApplicationProposal>>(await AddBookingStatusesAsync(proposals, ct));
    }

    public async Task<Result<IReadOnlyList<ApplicationProposal>, ApplicationError>> GetRecentDeniedForArtistAsync(
        CancellationToken ct = default)
    {
        var artistOption = await artistModule.GetCurrentProfileAsync(ct);
        if (!artistOption.TryGetValue(out var artist))
            return new ApplicationError.MissingArtist();

        var applications = await applicationRepository.GetByArtistTenantIdAndStateAsync(
            artist.TenantId,
            ApplicationState.Rejected,
            ct);
        var dtos = await resolver.ResolveProposalsAsync(applications, ct);
        var proposals = dtos.OrderByDescending(application => application.Opportunity.EndDate)
            .Take(5)
            .ToList();
        return new Success<IReadOnlyList<ApplicationProposal>>(await AddBookingStatusesAsync(proposals, ct));
    }

    public async Task<Result<IReadOnlyList<ApplicationProposal>, ApplicationError>> GetPendingForCurrentVenueAsync(
        CancellationToken ct = default)
    {
        if (tenantContext.TenantId is not { } tenantId)
            return new ApplicationError.MissingVenue();

        var applications = await applicationRepository.GetByVenueTenantIdAndStateAsync(
            tenantId,
            ApplicationState.Applied,
            ct);
        var now = timeProvider.GetUtcNow();
        var dtos = await resolver.ResolveProposalsAsync(applications, ct);
        var proposals = dtos.Where(application => application.Opportunity.EndDate > now)
            .OrderBy(application => application.Opportunity.StartDate)
            .ThenBy(application => application.Id)
            .Take(5)
            .ToList();
        return new Success<IReadOnlyList<ApplicationProposal>>(await AddBookingStatusesAsync(proposals, ct));
    }

    public async Task<Result<IReadOnlyList<ApplicationProposal>, ApplicationError>> GetCurrentForCurrentArtistAsync(
        CancellationToken ct = default)
    {
        if (tenantContext.TenantId is not { } tenantId)
            return new ApplicationError.MissingArtist();

        var applications = await applicationRepository.GetCurrentByArtistTenantIdAsync(tenantId, ct);
        var now = timeProvider.GetUtcNow();
        var dtos = await resolver.ResolveProposalsAsync(applications, ct);
        var proposals = dtos.Where(application => application.Opportunity.EndDate > now)
            .OrderBy(application => application.Opportunity.StartDate)
            .ThenBy(application => application.Id)
            .Take(10)
            .ToList();
        return new Success<IReadOnlyList<ApplicationProposal>>(await AddBookingStatusesAsync(proposals, ct));
    }

    public Task<Result<ApplicationProposal, ApplyApplicationError>> ApplyAsync(
        int opportunityId,
        ESignatureRequest eSignature,
        CancellationToken ct = default) =>
        workflow.ApplyAsync(opportunityId, eSignature, ct)
            .MapAsync(proposal => AddBookingStatusAsync(proposal, ct));

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
            return await transactionRunner.RunAsync<ApplicationService, UnitResult<WithdrawApplicationError>>(
                (service, token) => service.WithdrawCommandAsync(applicationId, actor, token),
                ct);
        }
        catch (DbUpdateException exception)
            when (exception.IsApplicationConcurrencyConflict(applicationId))
        {
            return await transactionRunner.RunAsync<ApplicationService, UnitResult<WithdrawApplicationError>>(
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
            return await transactionRunner.RunAsync<ApplicationService, UnitResult<RejectApplicationError>>(
                (service, token) => service.RejectCommandAsync(applicationId, actor, token),
                ct);
        }
        catch (DbUpdateException exception)
            when (exception.IsApplicationConcurrencyConflict(applicationId))
        {
            return await transactionRunner.RunAsync<ApplicationService, UnitResult<RejectApplicationError>>(
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
            return await transactionRunner.RunAsync<ApplicationService, UnitResult<CancelApplicationError>>(
                (service, token) => service.CancelCommandAsync(applicationId, actor, token),
                ct);
        }
        catch (DbUpdateException exception)
            when (exception.IsApplicationConcurrencyConflict(applicationId))
        {
            return await transactionRunner.RunAsync<ApplicationService, UnitResult<CancelApplicationError>>(
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
        if (await resources.CheckAsync(new AuthorizationRequest(
            TenantPermission.ApplicationsSubmit,
            ResourceAddress.Create(ResourceKind.Application, applicationId),
            ResourceFacet.Proposal), ct) != AuthorizationDecision.Allowed)
            return new WithdrawApplicationError.NotPermitted();
        if (await privilegedRepository.GetStateByIdAsync(applicationId, ct) == ApplicationState.Withdrawn)
            return new Success();

        return new WithdrawApplicationError.Superseded(applicationId);
    }

    private async Task<UnitResult<WithdrawApplicationError>> WithdrawCoreAsync(
        int applicationId,
        MembershipSnapshot expectedActor,
        CancellationToken ct)
    {
        authorizationContext.RegisterFailure<UnitResult<WithdrawApplicationError>>(
            () => new WithdrawApplicationError.NotPermitted());
        var parties = await privilegedRepository.GetNotificationTenantIdsAsync(applicationId, false, ct);
        if (parties.Count == 0)
            return new WithdrawApplicationError.ApplicationNotFound(applicationId);
        var resolutionOption = await tenantResolver.ResolveManyAsync(expectedActor, parties, ct);
        if (!resolutionOption.TryGetValue(out var resolution)
            || !resolution.ExistingTenantIds.SetEquals(parties)
            || await resources.RequireAsync(new AuthorizationRequest(
                TenantPermission.ApplicationsSubmit,
                ResourceAddress.Create(ResourceKind.Application, applicationId),
                ResourceFacet.Proposal), ct) != AuthorizationDecision.Allowed)
            return new WithdrawApplicationError.NotPermitted();
        var actor = resolution.Actor;

        var application = await privilegedRepository.GetByIdForUpdateAsync(applicationId, ct);
        if (application is null)
            return new WithdrawApplicationError.ApplicationNotFound(applicationId);
        if (application.ArtistTenantId != actor.TenantId
            || !parties.ToHashSet().SetEquals([application.VenueTenantId, application.ArtistTenantId]))
            return new WithdrawApplicationError.NotPermitted();
        if (application.Withdraw().TryGetError(out var transitionError))
            return new WithdrawApplicationError.InvalidTransition(transitionError);
        application.NotifyCounterparty(ApplicationNotification.Withdrawn);
        await notifier.WithdrawnAsync(application);
        return new Success();
    }

    private async Task<UnitResult<RejectApplicationError>> ClassifyRejectConflictAsync(
        int applicationId,
        CancellationToken ct)
    {
        if (await resources.CheckAsync(new AuthorizationRequest(
            TenantPermission.ApplicationsDecide,
            ResourceAddress.Create(ResourceKind.Application, applicationId),
            ResourceFacet.Proposal), ct) != AuthorizationDecision.Allowed)
            return new RejectApplicationError.NotPermitted();
        if (await privilegedRepository.GetStateByIdAsync(applicationId, ct) == ApplicationState.Rejected)
            return new Success();

        return new RejectApplicationError.Superseded(applicationId);
    }

    private async Task<UnitResult<RejectApplicationError>> RejectCoreAsync(
        int applicationId,
        MembershipSnapshot expectedActor,
        CancellationToken ct)
    {
        authorizationContext.RegisterFailure<UnitResult<RejectApplicationError>>(
            () => new RejectApplicationError.NotPermitted());
        var parties = await privilegedRepository.GetNotificationTenantIdsAsync(applicationId, false, ct);
        if (parties.Count == 0)
            return new RejectApplicationError.ApplicationNotFound(applicationId);
        var resolutionOption = await tenantResolver.ResolveManyAsync(expectedActor, parties, ct);
        if (!resolutionOption.TryGetValue(out var resolution)
            || !resolution.ExistingTenantIds.SetEquals(parties)
            || await resources.RequireAsync(new AuthorizationRequest(
                TenantPermission.ApplicationsDecide,
                ResourceAddress.Create(ResourceKind.Application, applicationId),
                ResourceFacet.Proposal), ct) != AuthorizationDecision.Allowed)
            return new RejectApplicationError.NotPermitted();
        var actor = resolution.Actor;

        var application = await privilegedRepository.GetByIdForUpdateAsync(applicationId, ct);
        if (application is null)
            return new RejectApplicationError.ApplicationNotFound(applicationId);
        if (application.VenueTenantId != actor.TenantId
            || !parties.ToHashSet().SetEquals([application.VenueTenantId, application.ArtistTenantId]))
            return new RejectApplicationError.NotPermitted();
        if (application.Reject().TryGetError(out var transitionError))
            return new RejectApplicationError.InvalidTransition(transitionError);
        application.NotifyCounterparty(ApplicationNotification.Rejected);
        await notifier.RejectedAsync(application);
        return new Success();
    }

    private async Task<UnitResult<CancelApplicationError>> ClassifyCancelConflictAsync(
        int applicationId,
        CancellationToken ct)
    {
        if (await resources.CheckAsync(new AuthorizationRequest(
            TenantPermission.ApplicationsDecide,
            ResourceAddress.Create(ResourceKind.Application, applicationId),
            ResourceFacet.Proposal), ct) != AuthorizationDecision.Allowed)
            return new CancelApplicationError.NotPermitted();
        if (await privilegedRepository.GetStateByIdAsync(applicationId, ct) == ApplicationState.Cancelled)
            return new Success();

        return new CancelApplicationError.Superseded(applicationId);
    }

    private async Task<UnitResult<CancelApplicationError>> CancelCoreAsync(
        int applicationId,
        MembershipSnapshot expectedActor,
        CancellationToken ct)
    {
        authorizationContext.RegisterFailure<UnitResult<CancelApplicationError>>(
            () => new CancelApplicationError.NotPermitted());
        var parties = await privilegedRepository.GetNotificationTenantIdsAsync(applicationId, false, ct);
        if (parties.Count == 0)
            return new CancelApplicationError.ApplicationNotFound(applicationId);
        var resolutionOption = await tenantResolver.ResolveManyAsync(expectedActor, parties, ct);
        if (!resolutionOption.TryGetValue(out var resolution)
            || !resolution.ExistingTenantIds.SetEquals(parties)
            || await resources.RequireAsync(new AuthorizationRequest(
                TenantPermission.ApplicationsDecide,
                ResourceAddress.Create(ResourceKind.Application, applicationId),
                ResourceFacet.Proposal), ct) != AuthorizationDecision.Allowed)
            return new CancelApplicationError.NotPermitted();
        var actor = resolution.Actor;

        var application = await privilegedRepository.GetByIdForUpdateAsync(applicationId, ct);
        if (application is null)
            return new CancelApplicationError.ApplicationNotFound(applicationId);
        if (application.VenueTenantId != actor.TenantId
            || !parties.ToHashSet().SetEquals([application.VenueTenantId, application.ArtistTenantId]))
            return new CancelApplicationError.NotPermitted();
        if (application.Cancel().TryGetError(out var transitionError))
            return new CancelApplicationError.InvalidTransition(transitionError);
        application.NotifyCounterparty(ApplicationNotification.ApplicationCancelled);
        await notifier.CancelledAsync(application);
        return new Success();
    }

    private async Task<UnitResult<ApplicationEligibilityError>> CheckCanApplyAsync(int opportunityId)
    {
        if (await permissions.CheckAsync(TenantPermission.ApplicationsSubmit, ResourceAudience.TenantResources)
            != AuthorizationDecision.Allowed)
            return new ApplicationEligibilityError.MissingArtist();
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
        if (applicationId <= 0 || await resources.CheckAsync(new AuthorizationRequest(
            TenantPermission.ApplicationsDecide,
            ResourceAddress.Create(ResourceKind.Application, applicationId),
            ResourceFacet.Proposal)) != AuthorizationDecision.Allowed)
            return new ApplicationEligibilityError.ApplicationNotFound();
        var application = await applicationRepository.GetDecisionByIdAsync(applicationId);
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

    private async Task<ApplicationSummary> AddBookingStatusAsync(ApplicationSummary summary, CancellationToken ct)
    {
        var bookingOption = await bookingModule.GetByApplicationIdAsync(summary.Id, ct);
        bookingOption.TryGetValue(out var booking);
        return summary with { BookingStatus = booking?.Status };
    }

    private async Task<ApplicationProposal> AddBookingStatusAsync(ApplicationProposal proposal, CancellationToken ct) =>
        (await AddBookingStatusesAsync([proposal], ct))[0];

    private async Task<IReadOnlyList<ApplicationProposal>> AddBookingStatusesAsync(
        IReadOnlyList<ApplicationProposal> proposals,
        CancellationToken ct)
    {
        if (proposals.Count == 0)
            return proposals;

        var result = new List<ApplicationProposal>(proposals.Count);
        foreach (var page in proposals.Chunk(100))
        {
            var ids = page.Select(proposal => proposal.Id).ToArray();
            var bookings = (await bookingModule.GetByApplicationIdsAsync(ids, ct))
                .ToDictionary(booking => booking.ApplicationId);
            var contracts = await bookingModule.GetContractIdsByApplicationIdsAsync(ids, ct);
            var canDecide = await applicationRepository.GetAllowedIdsAsync(
                ids, TenantPermission.ApplicationsDecide, ct);
            var canSubmit = await applicationRepository.GetAllowedIdsAsync(
                ids, TenantPermission.ApplicationsSubmit, ct);
            result.AddRange(page.Select(proposal =>
            {
                var status = bookings.GetValueOrDefault(proposal.Id)?.Status;
                var pending = proposal.State == ApplicationState.Applied;
                var decide = pending && canDecide.Contains(proposal.Id);
                return proposal with
                {
                    BookingStatus = status,
                    Actions = new ApplicationActionAvailability(
                        Accept: decide,
                        Checkout: decide && proposal.Opportunity.Deal.DealType.RequiresAcceptCheckout(),
                        Decline: decide,
                        Cancel: decide && status is null,
                        Withdraw: pending && canSubmit.Contains(proposal.Id),
                        Contract: contracts.ContainsKey(proposal.Id))
                };
            }));
        }
        return result;
    }

}
