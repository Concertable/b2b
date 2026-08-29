using System.Diagnostics;
using Concertable.B2B.Application.Application.DTOs;
using Concertable.B2B.Application.Application.Errors;
using Concertable.B2B.Application.Application.Mappers;
using Concertable.B2B.Application.Application.Requests;
using Concertable.B2B.Application.Application.Strategies;
using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Application.Domain.Events;
using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Opportunity.Contracts;
using Concertable.B2B.Venue.Contracts;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

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
    private readonly ITermsFingerprintCalculator termsFingerprint;
    private readonly IDealUnionFactory<Apply> applyFactory;
    private readonly IDealUnionFactory<Accept> acceptFactory;
    private readonly IApplicationMapper mapper;
    private readonly TimeProvider timeProvider;
    private readonly IUnitOfWorkBehavior unitOfWork;

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
        ITermsFingerprintCalculator termsFingerprint,
        IDealUnionFactory<Apply> applyFactory,
        IDealUnionFactory<Accept> acceptFactory,
        IApplicationMapper mapper,
        TimeProvider timeProvider,
        IUnitOfWorkBehavior unitOfWork)
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
        this.termsFingerprint = termsFingerprint;
        this.applyFactory = applyFactory;
        this.acceptFactory = acceptFactory;
        this.mapper = mapper;
        this.timeProvider = timeProvider;
        this.unitOfWork = unitOfWork;
    }

    public async Task<Result<ApplicationDto, ApplyApplicationError>> ApplyAsync(
        int opportunityId,
        string? paymentMethodId,
        ESignatureRequest eSignature)
    {
        var artistOption = await this.artistModule.GetCurrentProfileAsync();
        if (!artistOption.TryGetValue(out var artist))
            return new ApplyApplicationError.MissingArtist();

        if (tenantContext.TenantId is not { } artistTenantId)
            return new ApplyApplicationError.MissingTenant();

        var opportunityOption = await this.opportunityModule.GetOpenAsync(opportunityId);
        if (!opportunityOption.TryGetValue(out var opportunity))
            return new ApplyApplicationError.OpportunityNotFound(opportunityId);

        if (await applicationRepository.ExistsForOpportunityAndArtistTenantAsync(opportunityId, artist.TenantId))
            return new ApplyApplicationError.AlreadyApplied();

        var validation = await validator.CanApplyAsync(opportunity, artist.Id);
        if (validation.TryGetErrors(out var errors))
            return new ApplyApplicationError.Invalid(new ValidationErrors(errors.ToDictionary()));

        if (opportunity.Genres.Count > 0 && !artist.Genres.Overlaps(opportunity.Genres))
            return new ApplyApplicationError.GenreMismatch();

        var dealOption = await this.dealModule.GetByIdAsync(opportunity.DealId);
        if (!dealOption.TryGetValue(out var deal))
            return new ApplyApplicationError.OpportunityNotFound(opportunityId);

        ApplicationEntity application;
        switch (this.applyFactory.Create(deal.DealType))
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
            new Signature(
                userId,
                timeProvider.GetUtcNow().UtcDateTime,
                clientContext.IpAddress,
                clientContext.UserAgent,
                eSignature.SignatoryName,
                eSignature.DrawnSignatureImage),
            termsFingerprint.Calculate(
                deal,
                new DateRange(opportunity.StartDate, opportunity.EndDate)));
        application.NotifyCounterparty(ApplicationNotification.Applied);

        await applicationRepository.AddAsync(application);
        try
        {
            await applicationRepository.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (exception.IsDuplicateKey())
        {
            return new ApplyApplicationError.AlreadyApplied();
        }

        await notifier.AppliedAsync(application.Id);
        return await mapper.ToDtoAsync(application);
    }

    public Task<UnitResult<AcceptApplicationError>> AcceptAsync(
        int applicationId,
        string? paymentMethodId,
        ESignatureRequest eSignature,
        CancellationToken ct = default) =>
        unitOfWork.ExecuteAsync(
            () => AcceptCoreAsync(applicationId, paymentMethodId, eSignature, ct),
            ct);

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

        var eligibilityResult = await eligibility.CanAcceptAsync(application, ct)
            .MapError(error => (AcceptApplicationError)new AcceptApplicationError.Ineligible(error));
        if (eligibilityResult.TryGetError(out var eligibilityError))
            return eligibilityError;
        if (!eligibilityResult.TryGetValue(out var opportunity))
            throw new InvalidOperationException("Eligibility check succeeded without an opportunity value.");
        if (application.ValidateAccept().TryGetError(out var acceptError))
            return new AcceptApplicationError.InvalidTransition(acceptError);

        var dealOption = await this.dealModule.GetByIdAsync(opportunity.DealId, ct);
        if (!dealOption.TryGetValue(out var deal))
            return new AcceptApplicationError.Ineligible(
                new ApplicationEligibilityError.OpportunityNotFound());
        var artistOption = await this.artistModule.GetProfileAsync(application.ArtistId, ct);
        if (!artistOption.TryGetValue(out var artist))
            return new AcceptApplicationError.Ineligible(
                new ApplicationEligibilityError.ApplicationNotFound());
        var venueOption = await this.venueModule.GetProfileAsync(opportunity.VenueId, ct);
        if (!venueOption.TryGetValue(out var venue))
            return new AcceptApplicationError.Ineligible(
                new ApplicationEligibilityError.OpportunityNotFound());

        var fingerprint = termsFingerprint.Calculate(
            deal,
            new DateRange(opportunity.StartDate, opportunity.EndDate));
        if (application.TermsFingerprint != fingerprint)
            return new AcceptApplicationError.TermsChanged();

        if (currentUser.Id is not { } userId)
            return new AcceptApplicationError.Ineligible(
                new ApplicationEligibilityError.ApplicationNotFound());

        var operationId = application.AcceptanceOperationId ?? Guid.NewGuid();
        var venueSignature = new Signature(
            userId,
            timeProvider.GetUtcNow().UtcDateTime,
            clientContext.IpAddress,
            clientContext.UserAgent,
            eSignature.SignatoryName,
            eSignature.DrawnSignatureImage);
        Result<AcceptedApplication, AcceptApplicationError> accepted = this.acceptFactory.Create(deal.DealType) switch
        {
            Accept.Standard(var accept) => accept.Accept(
                application, opportunity, artist, venue, deal, venueSignature, operationId),
            Accept.Paid when string.IsNullOrWhiteSpace(paymentMethodId) =>
                new AcceptApplicationError.PaymentMethodRequired(),
            Accept.Paid(var accept) => accept.Accept(
                application, opportunity, artist, venue, deal, venueSignature, operationId, paymentMethodId),
            _ => throw new UnreachableException()
        };
        if (accepted.TryGetError(out var acceptanceError))
            return acceptanceError;
        if (!accepted.TryGetValue(out var acceptedApplication))
            throw new InvalidOperationException("Acceptance succeeded without an accepted application fact.");

        if ((await this.opportunityModule.FillAsync(
                application.OpportunityId,
                application.VenueTenantId,
                ct)).IsFailure)
            return new AcceptApplicationError.OpportunityUnavailable(application.OpportunityId);

        application.BeginAcceptance(operationId);
        if (application.Accept(acceptedApplication).TryGetError(out var transitionError))
            return new AcceptApplicationError.InvalidTransition(transitionError);
        application.NotifyCounterparty(ApplicationNotification.Accepted);
        await applicationRepository.SaveChangesAsync(ct);
        var rejectedApplicationIds = await applicationRepository.RejectAllExceptAsync(
            application.OpportunityId, application.Id, ct);
        foreach (var rejectedApplicationId in rejectedApplicationIds)
            await notifier.RejectedAsync(rejectedApplicationId);
        await notifier.AcceptedAsync(applicationId);
        return new Success();
    }
}
