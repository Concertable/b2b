using System.Diagnostics;
using Concertable.B2B.Application.Application.DTOs;
using Concertable.B2B.Application.Application.Errors;
using Concertable.B2B.Application.Application.Interfaces;
using Concertable.B2B.Application.Application.Mappers;
using Concertable.B2B.Application.Application.Requests;
using Concertable.B2B.Application.Application.Strategies;
using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Application.Domain;
using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Application.Domain.Events;
using Concertable.B2B.Application.Domain.Lifecycle;
using Concertable.B2B.Application.Infrastructure.Extensions;
using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Opportunity.Contracts;
using Concertable.B2B.Venue.Contracts;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Concertable.B2B.Application.Infrastructure.Specifications;
using Concertable.B2B.DataAccess.Infrastructure.Extensions;

namespace Concertable.B2B.Application.Infrastructure.Services;

internal sealed class ApplicationWorkflow : IApplicationWorkflow
{
    private readonly IApplicationRepository applicationRepository;
    private readonly IApplicationValidator validator;
    private readonly IApplicationNotifier notifier;
    private readonly IApplicationEligibility eligibility;
    private readonly IArtistModule artistModule;
    private readonly IOpportunityModule opportunityModule;
    private readonly IVenueModule venueModule;
    private readonly IDealModule dealModule;
    private readonly ITenantContext tenantContext;
    private readonly ICurrentUser currentUser;
    private readonly IClientContext clientContext;
    private readonly IDealStrategyFactory<IApplyStep> applyFactory;
    private readonly IDealStrategyFactory<ICommitmentReferenceStep> commitmentFactory;
    private readonly IApplicationMapper mapper;
    private readonly LegalSettings legal;
    private readonly TimeProvider timeProvider;
    private readonly IUnitOfWork unitOfWork;
    private readonly IUnitOfWorkBehavior unitOfWorkBehavior;

    public ApplicationWorkflow(
        IApplicationRepository applicationRepository,
        IApplicationValidator validator,
        IApplicationNotifier notifier,
        IApplicationEligibility eligibility,
        IArtistModule artistModule,
        IOpportunityModule opportunityModule,
        IVenueModule venueModule,
        IDealModule dealModule,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IClientContext clientContext,
        IDealStrategyFactory<IApplyStep> applyFactory,
        IDealStrategyFactory<ICommitmentReferenceStep> commitmentFactory,
        IApplicationMapper mapper,
        IOptions<LegalSettings> legal,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork,
        IUnitOfWorkBehavior unitOfWorkBehavior)
    {
        this.applicationRepository = applicationRepository;
        this.validator = validator;
        this.notifier = notifier;
        this.eligibility = eligibility;
        this.artistModule = artistModule;
        this.opportunityModule = opportunityModule;
        this.venueModule = venueModule;
        this.dealModule = dealModule;
        this.tenantContext = tenantContext;
        this.currentUser = currentUser;
        this.clientContext = clientContext;
        this.applyFactory = applyFactory;
        this.commitmentFactory = commitmentFactory;
        this.mapper = mapper;
        this.legal = legal.Value;
        this.timeProvider = timeProvider;
        this.unitOfWork = unitOfWork;
        this.unitOfWorkBehavior = unitOfWorkBehavior;
    }

    public async Task<Result<ApplicationDto, ApplyApplicationError>> ApplyAsync(
        int opportunityId,
        ESignatureRequest eSignature,
        CancellationToken ct = default)
    {
        var artistOption = await artistModule.GetCurrentProfileAsync(ct);
        if (!artistOption.TryGetValue(out var artist))
            return new ApplyApplicationError.MissingArtist();

        if (tenantContext.TenantId is not { } artistTenantId)
            return new ApplyApplicationError.MissingTenant();

        var opportunityOption = await opportunityModule.GetOpenAsync(opportunityId, ct);
        if (!opportunityOption.TryGetValue(out var opportunity))
            return new ApplyApplicationError.OpportunityNotFound(opportunityId);

        if (await applicationRepository.ExistsByOpportunityIdAndArtistTenantIdAsync(
                opportunityId, artist.TenantId, ct))
            return new ApplyApplicationError.AlreadyApplied();

        var validation = await validator.CanApplyAsync(opportunity, artist.Id, ct);
        if (validation.TryGetErrors(out var errors))
            return new ApplyApplicationError.Invalid(new ValidationErrors(errors.ToDictionary()));

        if (opportunity.Genres.Count > 0 && !artist.Genres.Overlaps(opportunity.Genres))
            return new ApplyApplicationError.GenreMismatch();

        var dealOption = await dealModule.GetByIdAsync(opportunity.DealId, ct);
        if (!dealOption.TryGetValue(out var deal))
            return new ApplyApplicationError.OpportunityNotFound(opportunityId);

        var applied = await applyFactory.Create(deal.DealType).ApplyAsync(
            artist.Id,
            opportunityId,
            deal.DealType,
            opportunity.VenueTenantId,
            artistTenantId,
            ct);
        if (applied.TryGetError(out var applyError))
            return applyError;
        if (!applied.TryGetValue(out var application))
            throw new InvalidOperationException("Apply succeeded without an application.");

        if (currentUser.Id is not { } userId)
            return new ApplyApplicationError.MissingUser();

        application.RecordArtistESignature(
            eSignature.ToSignature(userId, timeProvider.GetUtcNow().UtcDateTime, clientContext.IpAddress, clientContext.UserAgent),
            CalculateTermsFingerprint(deal, opportunity));
        application.NotifyCounterparty(ApplicationNotification.Applied);

        await applicationRepository.AddAsync(application, ct);
        if (!await unitOfWork.TrySaveChangesAsync(static exception => exception.IsDuplicateKey(), ct))
        {
            if (await applicationRepository.ExistsByOpportunityIdAndArtistTenantIdAsync(
                    opportunityId, artist.TenantId, ct))
                return new ApplyApplicationError.AlreadyApplied();

            throw new InvalidOperationException("Application save failed without creating an application.");
        }

        await notifier.AppliedAsync(application.Id);
        return await mapper.ToDtoAsync(application, ct);
    }

    public Task<UnitResult<AcceptApplicationError>> AcceptAsync(
        int applicationId,
        ESignatureRequest eSignature,
        CancellationToken ct = default) =>
        unitOfWorkBehavior.AttemptAsync(
            () => AcceptCoreAsync(applicationId, eSignature, ct),
            exception => exception.IsApplicationAcceptanceConflict(applicationId),
            _ => ClassifyAcceptConflictAsync(applicationId, ct),
            ct);

    private async Task<AttemptVerdict<UnitResult<AcceptApplicationError>>> ClassifyAcceptConflictAsync(
        int applicationId,
        CancellationToken ct)
    {
        var opportunityId = await applicationRepository.GetByIdAsync(
            applicationId,
            ApplicationSpecification.CreateOpportunityId(),
            ct);
        if (opportunityId is { } opportunity &&
            await applicationRepository.AnyAcceptedByOpportunityIdAsync(opportunity, ct))
            return new AttemptVerdict<UnitResult<AcceptApplicationError>>.Unrecoverable(
                new AcceptApplicationError.AlreadyAccepted());
        if (await applicationRepository.GetStateByIdAsync(applicationId, ct) is not ApplicationState.Applied)
            return new AttemptVerdict<UnitResult<AcceptApplicationError>>.Unrecoverable(
                new AcceptApplicationError.Superseded(applicationId));

        return new AttemptVerdict<UnitResult<AcceptApplicationError>>.Recoverable(
            new AcceptApplicationError.Contended(applicationId));
    }

    private async Task<UnitResult<AcceptApplicationError>> AcceptCoreAsync(
        int applicationId,
        ESignatureRequest eSignature,
        CancellationToken ct)
    {
        var application = await applicationRepository.GetByIdAsync(applicationId, ct);
        if (application is null)
            return new AcceptApplicationError.Ineligible(
                new ApplicationEligibilityError.ApplicationNotFound());

        // The application's own state gates first. Once it has left Applied the opportunity is
        // legitimately no longer open, and reporting that as an eligibility problem answers a lifecycle
        // conflict with a 400 about someone else's resource.
        if (application.ValidateAccept().TryGetError(out var acceptError))
            return new AcceptApplicationError.InvalidTransition(acceptError);

        var eligibilityResult = await eligibility.CanAcceptAsync(application, ct)
            .MapError(error => (AcceptApplicationError)new AcceptApplicationError.Ineligible(error));
        if (eligibilityResult.TryGetError(out var eligibilityError))
            return await applicationRepository.AnyAcceptedByOpportunityIdAsync(application.OpportunityId, ct)
                // A rival acceptance closes the opportunity, and reporting that as an eligibility problem
                // answers a lifecycle conflict with a 404 about someone else's resource.
                ? new AcceptApplicationError.AlreadyAccepted()
                : eligibilityError;
        if (!eligibilityResult.TryGetValue(out var opportunity))
            throw new InvalidOperationException("Eligibility check succeeded without an opportunity value.");

        var dealOption = await dealModule.GetByIdAsync(opportunity.DealId, ct);
        if (!dealOption.TryGetValue(out var deal))
            return new AcceptApplicationError.Ineligible(
                new ApplicationEligibilityError.OpportunityNotFound());
        var artistOption = await artistModule.GetProfileAsync(application.ArtistId, ct);
        if (!artistOption.TryGetValue(out var artist))
            return new AcceptApplicationError.Ineligible(
                new ApplicationEligibilityError.ApplicationNotFound());
        var venueOption = await venueModule.GetProfileAsync(opportunity.VenueId, ct);
        if (!venueOption.TryGetValue(out var venue))
            return new AcceptApplicationError.Ineligible(
                new ApplicationEligibilityError.OpportunityNotFound());

        if (application.TermsFingerprint != CalculateTermsFingerprint(deal, opportunity))
            return new AcceptApplicationError.TermsChanged();

        if (currentUser.Id is not { } userId)
            return new AcceptApplicationError.Ineligible(
                new ApplicationEligibilityError.ApplicationNotFound());

        var operationId = application.Acceptance.OperationId ?? Guid.NewGuid();
        var venueSignature = eSignature.ToSignature(
            userId, timeProvider.GetUtcNow().UtcDateTime, clientContext.IpAddress, clientContext.UserAgent);
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
        await unitOfWork.SaveChangesAsync(ct);

        var rejectedApplicationIds = await applicationRepository.RejectAllExceptAsync(
            application.OpportunityId, application.Id, ct);
        foreach (var rejectedApplicationId in rejectedApplicationIds)
            await notifier.RejectedAsync(rejectedApplicationId);
        await notifier.AcceptedAsync(applicationId);
        return new Success();
    }

    private static string CalculateTermsFingerprint(DealDto deal, OpportunityDto opportunity) =>
        ApplicationTermsFingerprint.Calculate(deal, new DateRange(opportunity.StartDate, opportunity.EndDate));
}
