using System.Diagnostics;
using Concertable.B2B.Application.Application.DTOs;
using Concertable.B2B.Application.Application.Errors;
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
using Concertable.Kernel.DependencyInjection;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;
using Concertable.B2B.Application.Infrastructure.Specifications;

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
    private readonly ITenantResolver tenantResolver;
    private readonly ICurrentUser currentUser;
    private readonly IClientContext clientContext;
    private readonly IDealUnionFactory<Apply> applyFactory;
    private readonly IDealUnionFactory<Accept> acceptFactory;
    private readonly IApplicationMapper mapper;
    private readonly TimeProvider timeProvider;
    private readonly IUnitOfWork unitOfWork;
    private readonly IUnitOfWorkBehavior unitOfWorkBehavior;
    private readonly IScoped<ApplicationWorkflow> acceptance;

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
        ITenantResolver tenantResolver,
        ICurrentUser currentUser,
        IClientContext clientContext,
        IDealUnionFactory<Apply> applyFactory,
        IDealUnionFactory<Accept> acceptFactory,
        IApplicationMapper mapper,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork,
        IUnitOfWorkBehavior unitOfWorkBehavior,
        IScoped<ApplicationWorkflow> acceptance)
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
        this.tenantResolver = tenantResolver;
        this.currentUser = currentUser;
        this.clientContext = clientContext;
        this.applyFactory = applyFactory;
        this.acceptFactory = acceptFactory;
        this.mapper = mapper;
        this.timeProvider = timeProvider;
        this.unitOfWork = unitOfWork;
        this.unitOfWorkBehavior = unitOfWorkBehavior;
        this.acceptance = acceptance;
    }

    public async Task<Result<ApplicationDto, ApplyApplicationError>> ApplyAsync(
        int opportunityId,
        string? paymentMethodId,
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

        ApplicationEntity application;
        switch (applyFactory.Create(deal.DealType))
        {
            case Apply.Standard(var apply):
                application = apply.Apply(
                    artist.Id,
                    opportunityId,
                    deal.DealType,
                    opportunity.VenueTenantId,
                    artistTenantId);
                break;
            case Apply.Prepaid when string.IsNullOrWhiteSpace(paymentMethodId):
                return new ApplyApplicationError.UnsupportedDeal(deal.DealType);
            case Apply.Prepaid(var apply):
                application = apply.Apply(
                    artist.Id,
                    opportunityId,
                    deal.DealType,
                    paymentMethodId,
                    opportunity.VenueTenantId,
                    artistTenantId);
                break;
            default:
                throw new UnreachableException();
        }

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
        string? paymentMethodId,
        ESignatureRequest eSignature,
        CancellationToken ct = default) =>
        unitOfWorkBehavior.TryExecuteAsync(
            () => AcceptCoreAsync(applicationId, paymentMethodId, eSignature, ct),
            exception => exception.IsApplicationAcceptanceConflict(applicationId),
            _ => ClassifyAcceptConflictAsync(applicationId, paymentMethodId, eSignature, ct),
            ct);

    internal async Task<UnitResult<AcceptApplicationError>> AcceptOnceAsync(
        int applicationId,
        string? paymentMethodId,
        ESignatureRequest eSignature,
        CancellationToken ct = default)
    {
        // A fresh scope has its own tenant context, and the middleware only resolved the one it created, so
        // without this every tenant-filtered read here answers as though the caller belonged to no tenant.
        await tenantResolver.ResolveAsync(ct);
        return await unitOfWorkBehavior.ExecuteAsync(
            () => AcceptCoreAsync(applicationId, paymentMethodId, eSignature, ct),
            ct);
    }

    private async Task<UnitResult<AcceptApplicationError>> ClassifyAcceptConflictAsync(
        int applicationId,
        string? paymentMethodId,
        ESignatureRequest eSignature,
        CancellationToken ct)
    {
        var opportunityId = await applicationRepository.GetByIdAsync(
            applicationId,
            ApplicationSpecification.CreateOpportunityId(),
            ct);
        if (opportunityId is { } opportunity &&
            await applicationRepository.AnyAcceptedByOpportunityIdAsync(opportunity, ct))
            return new AcceptApplicationError.AlreadyAccepted();
        if (await applicationRepository.GetStateByIdAsync(applicationId, ct) is not ApplicationState.Applied)
            return new AcceptApplicationError.Superseded(applicationId);

        // Nothing about the application forbids the acceptance, so the loss was to a change the acceptance
        // reads -- a payment verification landing mid-flight -- and rerunning in a FRESH scope decides on the
        // recorded outcome. The rerun does not rerun again, so a second loss is reported.
        return await acceptance.RunAsync(fresh =>
            fresh.AcceptOnceAsync(applicationId, paymentMethodId, eSignature, ct));
    }

    private async Task<UnitResult<AcceptApplicationError>> AcceptCoreAsync(
        int applicationId,
        string? paymentMethodId,
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

        var operationId = application.AcceptanceOperationId ?? Guid.NewGuid();
        var venueSignature = eSignature.ToSignature(
            userId, timeProvider.GetUtcNow().UtcDateTime, clientContext.IpAddress, clientContext.UserAgent);
        var accepted = acceptFactory.Create(deal.DealType) switch
        {
            Accept.Standard(var accept) => accept.Accept(
                application, opportunity, artist, venue, deal, venueSignature, operationId),
            Accept.Paid when string.IsNullOrWhiteSpace(paymentMethodId) =>
                new AcceptApplicationError.PaymentMethodRequired(),
            Accept.Paid(var accept) => accept.Accept(
                application, opportunity, artist, venue, deal, venueSignature, operationId, paymentMethodId)
        };
        if (accepted.TryGetError(out var acceptanceError))
            return acceptanceError;
        if (!accepted.TryGetValue(out var acceptedApplication))
            throw new InvalidOperationException("Acceptance succeeded without an accepted application fact.");

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
