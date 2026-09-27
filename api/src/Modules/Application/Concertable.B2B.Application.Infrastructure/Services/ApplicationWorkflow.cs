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
    private readonly IArtistCommandFacts artistFacts;
    private readonly IOpportunityCommandFacts opportunityFacts;
    private readonly IVenueCommandFacts venueFacts;
    private readonly IDealCommandFacts dealFacts;
    private readonly IClientContext clientContext;
    private readonly IDealStrategyFactory<IApplyStep> applyFactory;
    private readonly IDealStrategyFactory<ICommitmentReferenceStep> commitmentFactory;
    private readonly LegalSettings legal;
    private readonly TimeProvider timeProvider;
    private readonly IPrivilegedUnitOfWorkBehavior privilegedUnitOfWork;
    private readonly IMembershipContext membership;
    private readonly IMembershipAuthorityFence authorityFence;
    private readonly IPermissionCatalog permissionCatalog;
    private readonly ICommandExecutor commandExecutor;
    private readonly CommandTransactionAccessor transactions;

    public ApplicationWorkflow(
        IApplicationPrivilegedRepository privilegedRepository,
        IApplicationNotifier notifier,
        IArtistCommandFacts artistFacts,
        IOpportunityCommandFacts opportunityFacts,
        IVenueCommandFacts venueFacts,
        IDealCommandFacts dealFacts,
        IClientContext clientContext,
        IDealStrategyFactory<IApplyStep> applyFactory,
        IDealStrategyFactory<ICommitmentReferenceStep> commitmentFactory,
        IOptions<LegalSettings> legal,
        TimeProvider timeProvider,
        IPrivilegedUnitOfWorkBehavior privilegedUnitOfWork,
        IMembershipContext membership,
        IMembershipAuthorityFence authorityFence,
        IPermissionCatalog permissionCatalog,
        ICommandExecutor commandExecutor,
        CommandTransactionAccessor transactions)
    {
        this.privilegedRepository = privilegedRepository;
        this.notifier = notifier;
        this.artistFacts = artistFacts;
        this.opportunityFacts = opportunityFacts;
        this.venueFacts = venueFacts;
        this.dealFacts = dealFacts;
        this.clientContext = clientContext;
        this.applyFactory = applyFactory;
        this.commitmentFactory = commitmentFactory;
        this.legal = legal.Value;
        this.timeProvider = timeProvider;
        this.privilegedUnitOfWork = privilegedUnitOfWork;
        this.membership = membership;
        this.authorityFence = authorityFence;
        this.permissionCatalog = permissionCatalog;
        this.commandExecutor = commandExecutor;
        this.transactions = transactions;
    }

    public async Task<Result<ApplicationProposalDto, ApplyApplicationError>> ApplyAsync(
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
            return await commandExecutor.ExecuteAsync<ApplicationWorkflow, Result<ApplicationProposalDto, ApplyApplicationError>>(
                (workflow, token) => workflow.ClassifyApplyConflictAsync(opportunityId, actor, token),
                ct);
        }
    }

    private Task<Result<ApplicationProposalDto, ApplyApplicationError>> ExecuteApplyAsync(
        int opportunityId,
        ESignatureRequest eSignature,
        MembershipSnapshot actor,
        IPAddress ipAddress,
        string? userAgent,
        CancellationToken ct) =>
        commandExecutor.ExecuteAsync<ApplicationWorkflow, Result<ApplicationProposalDto, ApplyApplicationError>>(
            (workflow, token) => workflow.ApplyCommandAsync(
                opportunityId,
                eSignature,
                actor,
                ipAddress,
                userAgent,
                token),
            (workflow, result, token) => workflow.ValidateApplyAuthorityAsync(result, actor, token),
            () => (Result<ApplicationProposalDto, ApplyApplicationError>)new ApplyApplicationError.NotPermitted(),
            ct);

    private Task<Result<ApplicationProposalDto, ApplyApplicationError>> ApplyCommandAsync(
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

    private async Task<Result<ApplicationProposalDto, ApplyApplicationError>> ApplyCoreAsync(
        int opportunityId,
        ESignatureRequest eSignature,
        MembershipSnapshot expectedActor,
        IPAddress ipAddress,
        string? userAgent,
        CancellationToken ct)
    {
        var actor = await authorityFence.RequireCurrentAsync(expectedActor, ct);
        if (actor is null
            || !permissionCatalog.Grants(actor.Role, TenantPermission.ApplicationsSubmit))
            return new ApplyApplicationError.NotPermitted();

        var artist = await artistFacts.GetByTenantIdAsync(actor.TenantId, ct);
        if (artist is null)
            return new ApplyApplicationError.MissingArtist();

        var opportunity = await opportunityFacts.GetByIdAsync(opportunityId, ct);
        if (opportunity is null || !opportunity.IsOpen)
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

        var deal = await dealFacts.GetByIdAsync(opportunity.DealId, ct);
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
        await (transactions.Current
            ?? throw new InvalidOperationException("Apply requires an active command transaction."))
            .FlushAsync(ct);
        await notifier.AppliedAsync(application);

        var artistSummary = await artistFacts.GetSummaryByIdAsync(artist.Id, ct)
            ?? throw new InvalidOperationException($"Artist {artist.Id} disappeared during apply.");
        var venue = await venueFacts.GetByIdAsync(opportunity.VenueId, ct)
            ?? throw new InvalidOperationException($"Venue {opportunity.VenueId} disappeared during apply.");
        return new ApplicationProposalDto(
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

    private Task<Result<ApplicationProposalDto, ApplyApplicationError>> ClassifyApplyConflictAsync(
        int opportunityId,
        MembershipSnapshot expectedActor,
        CancellationToken ct) =>
        privilegedUnitOfWork.ExecuteAsync(async () =>
        {
            var actor = await authorityFence.RequireCurrentAsync(expectedActor, ct);
            if (actor is null
                || !permissionCatalog.Grants(actor.Role, TenantPermission.ApplicationsSubmit))
                return (Result<ApplicationProposalDto, ApplyApplicationError>)new ApplyApplicationError.NotPermitted();

            if (await privilegedRepository.ExistsByOpportunityIdAndArtistTenantIdAsync(
                    opportunityId,
                    actor.TenantId,
                    ct))
                return (Result<ApplicationProposalDto, ApplyApplicationError>)new ApplyApplicationError.AlreadyApplied();

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
        catch (DbUpdateException exception)
            when (exception.IsApplicationAcceptanceConflict(applicationId))
        {
            try
            {
                return await ExecuteAcceptAsync(applicationId, eSignature, actor, ct);
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
        commandExecutor.ExecuteAsync<ApplicationWorkflow, UnitResult<AcceptApplicationError>>(
            (workflow, token) => workflow.AcceptCommandAsync(
                applicationId,
                eSignature,
                actor,
                token),
            (workflow, _, token) => workflow.ValidateDecideAuthorityAsync(applicationId, actor, token),
            () => new AcceptApplicationError.NotPermitted(),
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
        var actor = await authorityFence.RequireCurrentAsync(expectedActor, ct);
        if (actor is null
            || !permissionCatalog.Grants(actor.Role, TenantPermission.ApplicationsDecide))
            return new AcceptApplicationError.NotPermitted();

        var application = await privilegedRepository.GetDecisionByIdForUpdateAsync(applicationId, ct);
        if (application is null)
            return new AcceptApplicationError.Ineligible(
                new ApplicationEligibilityError.ApplicationNotFound());

        var audience = permissionCatalog.AudienceFor(actor.Role, TenantPermission.ApplicationsDecide);
        if (application.VenueTenantId != actor.TenantId
            || !ResourceGrantPolicy.Allows(
                application.AccessGrants,
                ApplicationAccessScope.Proposal,
                actor,
                audience,
                timeProvider.GetUtcNow().UtcDateTime))
            return new AcceptApplicationError.NotPermitted();

        if (application.ValidateAccept().TryGetError(out var acceptError))
            return new AcceptApplicationError.InvalidTransition(acceptError);

        var opportunity = await opportunityFacts.GetByIdAsync(application.OpportunityId, ct);
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

        var deal = await dealFacts.GetByIdAsync(opportunity.DealId, ct);
        if (deal is null)
            return new AcceptApplicationError.Ineligible(
                new ApplicationEligibilityError.OpportunityNotFound());
        var artist = await artistFacts.GetByIdAsync(application.ArtistId, ct);
        if (artist is null)
            return new AcceptApplicationError.Ineligible(
                new ApplicationEligibilityError.ApplicationNotFound());
        var venue = await venueFacts.GetByIdAsync(opportunity.VenueId, ct);
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
        foreach (var rejectedApplication in rejectedApplications)
            await notifier.RejectedAsync(rejectedApplication);
        await notifier.AcceptedAsync(application);
        return new Success();
    }

    private static string CalculateTermsFingerprint(DealDto deal, OpportunityDto opportunity) =>
        ApplicationTermsFingerprint.Calculate(deal, new DateRange(opportunity.StartDate, opportunity.EndDate));

    private async Task<bool> ValidateApplyAuthorityAsync(
        Result<ApplicationProposalDto, ApplyApplicationError> result,
        MembershipSnapshot expectedActor,
        CancellationToken ct)
    {
        if (!result.TryGetValue(out var application))
            return true;

        return await ValidateSubmitAuthorityAsync(application.Id, expectedActor, ct);
    }

    private async Task<bool> ValidateSubmitAuthorityAsync(
        int applicationId,
        MembershipSnapshot expectedActor,
        CancellationToken ct)
    {
        var actor = await authorityFence.RequireCurrentAsync(expectedActor, ct);
        if (actor is null
            || !permissionCatalog.Grants(actor.Role, TenantPermission.ApplicationsSubmit))
            return false;

        return await privilegedRepository.CanSubmitAsync(
            applicationId,
            actor,
            permissionCatalog.AudienceFor(actor.Role, TenantPermission.ApplicationsSubmit),
            timeProvider.GetUtcNow().UtcDateTime,
            ct);
    }

    private async Task<bool> ValidateDecideAuthorityAsync(
        int applicationId,
        MembershipSnapshot expectedActor,
        CancellationToken ct)
    {
        var actor = await authorityFence.RequireCurrentAsync(expectedActor, ct);
        if (actor is null
            || !permissionCatalog.Grants(actor.Role, TenantPermission.ApplicationsDecide))
            return false;

        return await privilegedRepository.CanDecideAsync(
            applicationId,
            actor,
            permissionCatalog.AudienceFor(actor.Role, TenantPermission.ApplicationsDecide),
            timeProvider.GetUtcNow().UtcDateTime,
            ct);
    }
}
