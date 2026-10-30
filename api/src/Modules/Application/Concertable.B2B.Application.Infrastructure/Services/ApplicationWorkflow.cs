using System.Net;
using Concertable.B2B.Application.Application.DTOs;
using Concertable.B2B.Application.Application.Errors;
using Concertable.B2B.Application.Application.Interfaces;
using Concertable.B2B.Application.Application.Mappers;
using Concertable.B2B.Application.Application.Requests;
using Concertable.B2B.Application.Application.Strategies;
using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Application.Contracts.Enums;
using Concertable.B2B.Application.Domain;
using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Application.Domain.Events;
using Concertable.B2B.Application.Domain.Lifecycle;
using Concertable.B2B.Application.Infrastructure.Extensions;
using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Opportunity.Contracts;
using Concertable.B2B.Tenant.Contracts;
using ITenantResolver = Concertable.B2B.Tenant.Contracts.ITenantResolver;
using Concertable.B2B.Venue.Contracts;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Application.Infrastructure.Services;

internal sealed class ApplicationWorkflow : IApplicationWorkflow
{
    private readonly IApplicationPrivilegedRepository privilegedRepository;
    private readonly IApplicationNotifier notifier;
    private readonly IArtistPrivilegedReadRepository artistRepository;
    private readonly IOpportunityPrivilegedReadRepository opportunityRepository;
    private readonly IVenuePrivilegedReadRepository venueRepository;
    private readonly IDealPrivilegedReadRepository dealRepository;
    private readonly IClientContext clientContext;
    private readonly IDealStrategyFactory<IApplyStep> applyFactory;
    private readonly IDealStrategyFactory<ICommitmentReferenceStep> commitmentFactory;
    private readonly LegalSettings legal;
    private readonly TimeProvider timeProvider;
    private readonly IPrivilegedUnitOfWorkBehavior privilegedUnitOfWork;
    private readonly IMembershipContext membership;
    private readonly IAuthorizationContext authorizationContext;
    private readonly ITenantResolver tenantResolver;
    private readonly IPermissionAuthorization permissions;
    private readonly IResourceAuthorization resources;
    private readonly ITransactionRunner transactionRunner;
    private readonly UnitOfWorkAccessor unitOfWorkAccessor;

    public ApplicationWorkflow(
        IApplicationPrivilegedRepository privilegedRepository,
        IApplicationNotifier notifier,
        IArtistPrivilegedReadRepository artistRepository,
        IOpportunityPrivilegedReadRepository opportunityRepository,
        IVenuePrivilegedReadRepository venueRepository,
        IDealPrivilegedReadRepository dealRepository,
        IClientContext clientContext,
        IDealStrategyFactory<IApplyStep> applyFactory,
        IDealStrategyFactory<ICommitmentReferenceStep> commitmentFactory,
        IOptions<LegalSettings> legal,
        TimeProvider timeProvider,
        IPrivilegedUnitOfWorkBehavior privilegedUnitOfWork,
        IMembershipContext membership,
        IAuthorizationContext authorizationContext,
        ITenantResolver tenantResolver,
        IPermissionAuthorization permissions,
        IResourceAuthorization resources,
        ITransactionRunner transactionRunner,
        UnitOfWorkAccessor unitOfWorkAccessor)
    {
        this.privilegedRepository = privilegedRepository;
        this.notifier = notifier;
        this.artistRepository = artistRepository;
        this.opportunityRepository = opportunityRepository;
        this.venueRepository = venueRepository;
        this.dealRepository = dealRepository;
        this.clientContext = clientContext;
        this.applyFactory = applyFactory;
        this.commitmentFactory = commitmentFactory;
        this.legal = legal.Value;
        this.timeProvider = timeProvider;
        this.privilegedUnitOfWork = privilegedUnitOfWork;
        this.membership = membership;
        this.authorizationContext = authorizationContext;
        this.tenantResolver = tenantResolver;
        this.permissions = permissions;
        this.resources = resources;
        this.transactionRunner = transactionRunner;
        this.unitOfWorkAccessor = unitOfWorkAccessor;
    }

    public async Task<Result<ApplicationProposal, ApplyApplicationError>> ApplyAsync(
        int opportunityId,
        ESignatureRequest eSignature,
        CancellationToken ct = default)
    {
        if (membership.Membership is not { } actor)
            return new ApplyApplicationError.NotPermitted();

        var ipAddress = clientContext.IpAddress;
        var userAgent = clientContext.UserAgent;
        try
        {
            return await ExecuteApplyAsync(
                opportunityId,
                eSignature,
                actor,
                ipAddress,
                userAgent,
                ct);
        }
        catch (DbUpdateException exception) when (exception.IsDuplicateKey())
        {
            return await transactionRunner.RunAsync<ApplicationWorkflow, Result<ApplicationProposal, ApplyApplicationError>>(
                (workflow, token) => workflow.ClassifyApplyConflictAsync(opportunityId, actor, token),
                ct);
        }
    }

    private Task<Result<ApplicationProposal, ApplyApplicationError>> ExecuteApplyAsync(
        int opportunityId,
        ESignatureRequest eSignature,
        MembershipSnapshot actor,
        IPAddress ipAddress,
        string? userAgent,
        CancellationToken ct) =>
        transactionRunner.RunAsync<ApplicationWorkflow, Result<ApplicationProposal, ApplyApplicationError>>(
            (workflow, token) => workflow.ApplyCommandAsync(
                opportunityId,
                eSignature,
                actor,
                ipAddress,
                userAgent,
                token),
            ct);

    private Task<Result<ApplicationProposal, ApplyApplicationError>> ApplyCommandAsync(
        int opportunityId,
        ESignatureRequest eSignature,
        MembershipSnapshot actor,
        IPAddress ipAddress,
        string? userAgent,
        CancellationToken ct) =>
        privilegedUnitOfWork.ExecuteAsync(
            () => ApplyCoreAsync(
                opportunityId,
                eSignature,
                actor,
                ipAddress,
                userAgent,
                ct),
            ct);

    private async Task<Result<ApplicationProposal, ApplyApplicationError>> ApplyCoreAsync(
        int opportunityId,
        ESignatureRequest eSignature,
        MembershipSnapshot expectedActor,
        IPAddress ipAddress,
        string? userAgent,
        CancellationToken ct)
    {
        authorizationContext.RegisterFailure<Result<ApplicationProposal, ApplyApplicationError>>(
            () => new ApplyApplicationError.NotPermitted());
        if (!expectedActor.HasPermission(TenantPermission.ApplicationsSubmit))
            return new ApplyApplicationError.NotPermitted();
        var proposedOpportunity = await opportunityRepository.GetByIdAsync(opportunityId, ct);
        if (proposedOpportunity is null)
            return new ApplyApplicationError.OpportunityNotFound(opportunityId);
        var parties = new[] { expectedActor.TenantId, proposedOpportunity.VenueTenantId };
        var resolutionOption = await tenantResolver.ResolveManyAsync(expectedActor, parties, ct);
        if (!resolutionOption.TryGetValue(out var resolution)
            || !resolution.ExistingTenantIds.SetEquals(parties)
            || await permissions.RequireAsync(TenantPermission.ApplicationsSubmit, ResourceAudience.TenantResources, ct)
                != AuthorizationDecision.Allowed)
            return new ApplyApplicationError.NotPermitted();
        var actor = resolution.Actor;
        await privilegedRepository.LockOpportunityAsync(opportunityId, ct);
        if (await privilegedRepository.AnyAcceptedByOpportunityIdAsync(opportunityId, ct))
            return new ApplyApplicationError.OpportunityNotFound(opportunityId);

        var artist = await artistRepository.GetByTenantIdAsync(actor.TenantId, ct);
        if (artist is null)
            return new ApplyApplicationError.MissingArtist();

        var opportunity = await opportunityRepository.GetByIdAsync(opportunityId, ct);
        if (opportunity is null || !opportunity.IsOpen
            || opportunity.VenueTenantId != proposedOpportunity.VenueTenantId)
            return new ApplyApplicationError.OpportunityNotFound(opportunityId);

        if (await privilegedRepository.ExistsByOpportunityIdAndArtistTenantIdAsync(
                opportunityId,
                actor.TenantId,
                ct))
            return new ApplyApplicationError.AlreadyApplied();

        var validationErrors = new List<string>();
        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (opportunity.StartDate < now)
            validationErrors.Add("This concert opportunity has already passed");
        if (await privilegedRepository.OpportunityHasConcertAsync(opportunity.Id, ct))
            validationErrors.Add("This concert opportunity has already been booked for a concert");
        if (await privilegedRepository.ArtistHasConcertOnDateAsync(artist.Id, opportunity.StartDate, ct))
            validationErrors.Add("You already have a concert on this day");
        if (validationErrors.Count > 0)
            return new ApplyApplicationError.Invalid(new ValidationErrors(
                new Dictionary<string, string[]> { ["application"] = validationErrors.ToArray() }));

        if (opportunity.Genres.Count > 0 && !artist.Genres.Overlaps(opportunity.Genres))
            return new ApplyApplicationError.GenreMismatch();

        var deal = await dealRepository.GetByIdAsync(opportunity.DealId, ct);
        if (deal is null)
            return new ApplyApplicationError.OpportunityNotFound(opportunityId);

        var applied = await applyFactory.Create(deal.DealType).ApplyAsync(
            artist.Id,
            opportunityId,
            deal.DealType,
            opportunity.VenueTenantId,
            actor.TenantId,
            now,
            ct);
        if (applied.TryGetError(out var applyError))
            return applyError;
        if (!applied.TryGetValue(out var application))
            throw new InvalidOperationException("Apply succeeded without an application.");

        application.RecordArtistESignature(
            eSignature.ToSignature(actor.UserId, now, ipAddress, userAgent),
            CalculateTermsFingerprint(deal, opportunity));
        application.NotifyCounterparty(ApplicationNotification.Applied);
        await privilegedRepository.AddAsync(application, ct);
        await (unitOfWorkAccessor.Current
            ?? throw new InvalidOperationException("Apply requires an active unit of work."))
            .FlushAsync(ct);
        authorizationContext.RegisterValidator(async token =>
            await resources.CheckAsync(new AuthorizationRequest(
                TenantPermission.ApplicationsSubmit,
                ResourceAddress.Create(ResourceKind.Application, application.Id),
                ResourceFacet.Proposal), token) == AuthorizationDecision.Allowed);
        await notifier.AppliedAsync(application);

        var artistSummary = await artistRepository.GetSummaryByIdAsync(artist.Id, ct)
            ?? throw new InvalidOperationException($"Artist {artist.Id} disappeared during apply.");
        var venue = await venueRepository.GetByIdAsync(opportunity.VenueId, ct)
            ?? throw new InvalidOperationException($"Venue {opportunity.VenueId} disappeared during apply.");
        return new ApplicationProposal(
            application.Id,
            application.VenueTenantId,
            application.ArtistTenantId,
            artistSummary,
            new OpportunityProposal(
                opportunity.Id,
                opportunity.VenueId,
                venue.Name,
                opportunity.StartDate,
                opportunity.EndDate,
                opportunity.Genres,
                deal),
            application.State.ToStatus(),
            application.State);
    }

    private Task<Result<ApplicationProposal, ApplyApplicationError>> ClassifyApplyConflictAsync(
        int opportunityId,
        MembershipSnapshot expectedActor,
        CancellationToken ct) =>
        privilegedUnitOfWork.ExecuteAsync(async () =>
        {
            if (await permissions.CheckAsync(TenantPermission.ApplicationsSubmit, ResourceAudience.TenantResources, ct)
                != AuthorizationDecision.Allowed)
                return (Result<ApplicationProposal, ApplyApplicationError>)new ApplyApplicationError.NotPermitted();

            if (await privilegedRepository.ExistsByOpportunityIdAndArtistTenantIdAsync(
                    opportunityId,
                    expectedActor.TenantId,
                    ct))
                return (Result<ApplicationProposal, ApplyApplicationError>)new ApplyApplicationError.AlreadyApplied();

            throw new InvalidOperationException("Application save failed without creating an application.");
        }, ct);

    public async Task<UnitResult<AcceptApplicationError>> AcceptAsync(
        int applicationId,
        ESignatureRequest eSignature,
        CancellationToken ct = default)
    {
        if (membership.Membership is not { } actor)
            return new AcceptApplicationError.NotPermitted();

        try
        {
            return await ExecuteAcceptAsync(applicationId, eSignature, actor, ct);
        }
        catch (TenantUnavailableException)
        {
            return new AcceptApplicationError.PartyUnavailable();
        }
        catch (DbUpdateException exception)
            when (exception.IsApplicationAcceptanceConflict(applicationId))
        {
            try
            {
                return await ExecuteAcceptAsync(applicationId, eSignature, actor, ct);
            }
            catch (TenantUnavailableException)
            {
                return new AcceptApplicationError.PartyUnavailable();
            }
            catch (DbUpdateException retryException)
                when (retryException.IsApplicationAcceptanceConflict(applicationId))
            {
                return new AcceptApplicationError.Superseded(applicationId);
            }
        }
    }

    private Task<UnitResult<AcceptApplicationError>> ExecuteAcceptAsync(
        int applicationId,
        ESignatureRequest eSignature,
        MembershipSnapshot actor,
        CancellationToken ct) =>
        transactionRunner.RunAsync<ApplicationWorkflow, UnitResult<AcceptApplicationError>>(
            (workflow, token) => workflow.AcceptCommandAsync(
                applicationId,
                eSignature,
                actor,
                token),
            ct);

    private Task<UnitResult<AcceptApplicationError>> AcceptCommandAsync(
        int applicationId,
        ESignatureRequest eSignature,
        MembershipSnapshot actor,
        CancellationToken ct) =>
        privilegedUnitOfWork.ExecuteAsync(
            () => AcceptCoreAsync(applicationId, eSignature, actor, ct),
            ct);

    private async Task<UnitResult<AcceptApplicationError>> AcceptCoreAsync(
        int applicationId,
        ESignatureRequest eSignature,
        MembershipSnapshot expectedActor,
        CancellationToken ct)
    {
        authorizationContext.RegisterFailure<UnitResult<AcceptApplicationError>>(
            () => new AcceptApplicationError.NotPermitted());
        var opportunityId = await privilegedRepository.GetOpportunityIdAsync(applicationId, ct);
        var parties = await privilegedRepository.GetNotificationTenantIdsAsync(applicationId, true, ct);
        if (opportunityId is null || parties.Count == 0)
            return new AcceptApplicationError.Ineligible(
                new ApplicationEligibilityError.ApplicationNotFound());
        var resolutionOption = await tenantResolver.ResolveManyAsync(expectedActor, parties, ct);
        if (!resolutionOption.TryGetValue(out var resolution)
            || !resolution.ExistingTenantIds.SetEquals(parties))
            return new AcceptApplicationError.NotPermitted();
        await privilegedRepository.LockOpportunityAsync(opportunityId.Value, ct);
        var fencedParties = await privilegedRepository.GetNotificationTenantIdsAsync(applicationId, true, ct);
        if (!parties.ToHashSet().SetEquals(fencedParties))
            return new AcceptApplicationError.Superseded(applicationId);
        if (await resources.RequireAsync(new AuthorizationRequest(
                TenantPermission.ApplicationsDecide,
                ResourceAddress.Create(ResourceKind.Application, applicationId),
                ResourceFacet.Proposal), ct) != AuthorizationDecision.Allowed)
            return new AcceptApplicationError.NotPermitted();
        var actor = resolution.Actor;

        var application = await privilegedRepository.GetDecisionByIdForUpdateAsync(applicationId, ct);
        if (application is null)
            return new AcceptApplicationError.Ineligible(
                new ApplicationEligibilityError.ApplicationNotFound());

        var currentParties = await privilegedRepository.GetNotificationTenantIdsAsync(applicationId, true, ct);
        if (application.VenueTenantId != actor.TenantId
            || !parties.ToHashSet().SetEquals(currentParties))
            return new AcceptApplicationError.NotPermitted();

        if (application.ValidateAccept().TryGetError(out var acceptError))
            return new AcceptApplicationError.InvalidTransition(acceptError);

        var opportunity = await opportunityRepository.GetByIdAsync(application.OpportunityId, ct);
        if (opportunity is null)
            return new AcceptApplicationError.Ineligible(
                new ApplicationEligibilityError.OpportunityNotFound());

        var validationErrors = new List<string>();
        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (opportunity.VenueTenantId != actor.TenantId)
            validationErrors.Add("You do not own this concert opportunity");
        if (!opportunity.IsOpen)
            validationErrors.Add("This concert opportunity is no longer open");
        if (opportunity.StartDate < now)
            validationErrors.Add("This concert opportunity has already passed");
        if (await privilegedRepository.OpportunityHasConcertAsync(opportunity.Id, ct))
            validationErrors.Add("This concert opportunity already has a concert booked");
        if (await privilegedRepository.ArtistHasConcertOnDateAsync(
                application.ArtistId,
                opportunity.StartDate,
                ct))
            validationErrors.Add("This artist already has a concert on this day");
        if (await privilegedRepository.VenueHasConcertOnDateAsync(
                opportunity.VenueId,
                opportunity.StartDate,
                ct))
            validationErrors.Add("You already have a concert on this day");
        if (validationErrors.Count > 0)
        {
            if (await privilegedRepository.AnyAcceptedByOpportunityIdAsync(application.OpportunityId, ct))
                return new AcceptApplicationError.AlreadyAccepted();

            return new AcceptApplicationError.Ineligible(
                new ApplicationEligibilityError.Invalid(new ValidationErrors(
                    new Dictionary<string, string[]> { ["application"] = validationErrors.ToArray() })));
        }

        var deal = await dealRepository.GetByIdAsync(opportunity.DealId, ct);
        if (deal is null)
            return new AcceptApplicationError.Ineligible(
                new ApplicationEligibilityError.OpportunityNotFound());
        var artist = await artistRepository.GetByIdAsync(application.ArtistId, ct);
        if (artist is null)
            return new AcceptApplicationError.Ineligible(
                new ApplicationEligibilityError.ApplicationNotFound());
        var venue = await venueRepository.GetByIdAsync(opportunity.VenueId, ct);
        if (venue is null)
            return new AcceptApplicationError.Ineligible(
                new ApplicationEligibilityError.OpportunityNotFound());

        if (application.TermsFingerprint != CalculateTermsFingerprint(deal, opportunity))
            return new AcceptApplicationError.TermsChanged();

        var operationId = application.AcceptanceOperationId ?? Guid.NewGuid();
        var venueSignature = eSignature.ToSignature(
            actor.UserId,
            timeProvider.GetUtcNow().UtcDateTime,
            clientContext.IpAddress,
            clientContext.UserAgent);
        var snapshot = new ApplicationAcceptanceSnapshot(
            operationId,
            new ApplicationSnapshot(
                application.Id,
                new ArtistSnapshot(
                    application.ArtistId,
                    application.ArtistTenantId,
                    artist.Name),
                new OpportunitySnapshot(
                    application.OpportunityId,
                    new VenueSnapshot(
                        opportunity.VenueId,
                        application.VenueTenantId,
                        venue.Name),
                    opportunity.StartDate,
                    opportunity.EndDate,
                    opportunity.Genres.ToList())),
            new ContractSnapshot(
                deal.PaymentMethod,
                deal.Terms.Render(),
                legal.PlatformTermsVersion,
                legal.MandateTermsVersion,
                commitmentFactory.Create(deal.DealType).Resolve(application),
                application.ArtistESignature,
                venueSignature,
                deal.Terms));
        var acceptedApplication = new AcceptedApplication(snapshot);

        application.BeginAcceptance(operationId);
        if (application.Accept(acceptedApplication).TryGetError(out var transitionError))
            return new AcceptApplicationError.InvalidTransition(transitionError);
        application.NotifyCounterparty(ApplicationNotification.Accepted);
        var rejectedApplications = await privilegedRepository.RejectAllExceptAsync(
            application.OpportunityId, application.Id, ct);
        if (rejectedApplications.Any(rejected => !parties.Contains(rejected.ArtistTenantId)))
            return new AcceptApplicationError.NotPermitted();
        foreach (var rejectedApplication in rejectedApplications)
            await notifier.RejectedAsync(rejectedApplication);
        await notifier.AcceptedAsync(application);
        return new Success();
    }

    private static string CalculateTermsFingerprint(DealDto deal, OpportunityDto opportunity) =>
        ApplicationTermsFingerprint.Calculate(deal, new DateRange(opportunity.StartDate, opportunity.EndDate));

}
