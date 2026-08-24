using System.Diagnostics;
using Concertable.B2B.Application.Application.Errors;
using Concertable.B2B.Application.Application.Mappers;
using Concertable.B2B.Application.Application.Steps;
using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Application.Domain.Events;
using Concertable.B2B.Application.Domain.State;
using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Opportunity.Contracts;
using Concertable.B2B.Venue.Contracts;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Application.Infrastructure.Services;

internal sealed class ApplicationService : IApplicationService
{
    private readonly IApplicationRepository repository;
    private readonly IApplicationValidator validator;
    private readonly IApplicationNotifier notifier;
    private readonly IArtistModule artists;
    private readonly IOpportunityModule opportunities;
    private readonly IVenueModule venues;
    private readonly IDealModule deals;
    private readonly ITenantContext tenantContext;
    private readonly ICurrentUser currentUser;
    private readonly IClientContext clientContext;
    private readonly ITermsFingerprintCalculator termsFingerprint;
    private readonly IDealTermsRenderer termsRenderer;
    private readonly IAcceptFactory acceptFactory;
    private readonly IApplicationCheckoutService checkout;
    private readonly IApplicationMapper mapper;
    private readonly TimeProvider timeProvider;
    private readonly LegalSettings legal;
    private readonly IUnitOfWorkBehavior unitOfWork;

    public ApplicationService(
        IApplicationRepository repository,
        IApplicationValidator validator,
        IApplicationNotifier notifier,
        IArtistModule artists,
        IOpportunityModule opportunities,
        IVenueModule venues,
        IDealModule deals,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IClientContext clientContext,
        ITermsFingerprintCalculator termsFingerprint,
        IDealTermsRenderer termsRenderer,
        IAcceptFactory acceptFactory,
        IApplicationCheckoutService checkout,
        IApplicationMapper mapper,
        TimeProvider timeProvider,
        IOptions<LegalSettings> legal,
        IUnitOfWorkBehavior unitOfWork)
    {
        this.repository = repository;
        this.validator = validator;
        this.notifier = notifier;
        this.artists = artists;
        this.opportunities = opportunities;
        this.venues = venues;
        this.deals = deals;
        this.tenantContext = tenantContext;
        this.currentUser = currentUser;
        this.clientContext = clientContext;
        this.termsFingerprint = termsFingerprint;
        this.termsRenderer = termsRenderer;
        this.acceptFactory = acceptFactory;
        this.checkout = checkout;
        this.mapper = mapper;
        this.timeProvider = timeProvider;
        this.legal = legal.Value;
        this.unitOfWork = unitOfWork;
    }

    public Task<Result<ApplicationDto, ApplicationError>> GetByIdAsync(int id) =>
        repository.GetByIdAsync(id)
            .ToOption()
            .OrFailure(() => (ApplicationError)new ApplicationError.NotFound(id))
            .MapAsync(mapper.ToDtoAsync);

    public async Task<Result<IReadOnlyList<ApplicationDto>, ApplicationError>> GetByOpportunityIdAsync(int id)
    {
        var opportunityOption = await opportunities.GetDetailsAsync(id);
        if (!opportunityOption.TryGetValue(out var opportunity) ||
            opportunity.VenueTenantId != tenantContext.TenantId)
            return new ApplicationError.OpportunityForbidden(id);

        var applications = await repository.GetByOpportunityIdAsync(id);
        return new Success<IReadOnlyList<ApplicationDto>>(await mapper.ToDtosAsync(applications));
    }

    public async Task<Result<IReadOnlyList<ApplicationDto>, ApplicationError>> GetPendingForArtistAsync()
    {
        var artistOption = await artists.GetCurrentProfileAsync();
        if (!artistOption.TryGetValue(out var artist))
            return new ApplicationError.MissingArtist();

        var applications = await repository.GetByArtistTenantIdAndStateAsync(
            artist.TenantId,
            ApplicationState.Applied);
        var dtos = await mapper.ToDtosAsync(applications);
        return new Success<IReadOnlyList<ApplicationDto>>(
            dtos.Where(application => application.Opportunity.StartDate > timeProvider.GetUtcNow())
                .ToList());
    }

    public async Task<Result<IReadOnlyList<ApplicationDto>, ApplicationError>> GetRecentDeniedForArtistAsync()
    {
        var artistOption = await artists.GetCurrentProfileAsync();
        if (!artistOption.TryGetValue(out var artist))
            return new ApplicationError.MissingArtist();

        var applications = await repository.GetByArtistTenantIdAndStateAsync(
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

        var applications = await repository.GetByVenueTenantIdAndStateAsync(
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

        var applications = await repository.GetCurrentByArtistTenantIdAsync(tenantId);
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
        ESignatureRequest eSignature) =>
        ApplyAsync(opportunityId, null, eSignature);

    public async Task<Result<ApplicationDto, ApplyApplicationError>> ApplyAsync(
        int opportunityId,
        string? paymentMethodId,
        ESignatureRequest eSignature)
    {
        var artistOption = await artists.GetCurrentProfileAsync();
        if (!artistOption.TryGetValue(out var artist))
            return new ApplyApplicationError.MissingArtist();

        if (tenantContext.TenantId is not { } artistTenantId)
            return new ApplyApplicationError.MissingTenant();

        var opportunityOption = await opportunities.GetOpenDetailsAsync(opportunityId);
        if (!opportunityOption.TryGetValue(out var opportunity))
            return new ApplyApplicationError.OpportunityNotFound(opportunityId);

        if (await repository.ExistsForOpportunityAndArtistTenantAsync(opportunityId, artist.TenantId))
            return new ApplyApplicationError.AlreadyApplied();

        var validation = await validator.CanApplyAsync(opportunity, artist.Id);
        if (validation.TryGetErrors(out var errors))
            return new ApplyApplicationError.Invalid(new ValidationErrors(errors.ToDictionary()));

        if (opportunity.Genres.Count > 0 && !artist.Genres.Overlaps(opportunity.Genres))
            return new ApplyApplicationError.GenreMismatch();

        var dealOption = await deals.GetByIdAsync(opportunity.DealId);
        if (!dealOption.TryGetValue(out var deal))
            return new ApplyApplicationError.OpportunityNotFound(opportunityId);

        if (deal.DealType == DealType.VenueHire && string.IsNullOrWhiteSpace(paymentMethodId))
            return new ApplyApplicationError.UnsupportedDeal(deal.DealType);

        ApplicationEntity application = deal.DealType == DealType.VenueHire
            ? PrepaidApplication.Create(
                artist.Id,
                opportunityId,
                deal.DealType,
                paymentMethodId!,
                opportunity.VenueTenantId,
                artistTenantId)
            : StandardApplication.Create(
                artist.Id,
                opportunityId,
                deal.DealType,
                opportunity.VenueTenantId,
                artistTenantId);

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

        await repository.AddAsync(application);
        try
        {
            await repository.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (exception.IsDuplicateKey())
        {
            return new ApplyApplicationError.AlreadyApplied();
        }

        await notifier.AppliedAsync(application.Id);
        return await mapper.ToDtoAsync(application);
    }

    public async Task<bool> CanApplyAsync(int opportunityId) =>
        (await CheckCanApplyAsync(opportunityId)).IsSuccess;

    public async Task<bool> CanAcceptAsync(int applicationId) =>
        (await CheckCanAcceptAsync(applicationId)).IsSuccess;

    public async Task<Result<Checkout, ApplicationCheckoutError>> ApplyCheckoutAsync(int opportunityId)
    {
        var eligibility = await CheckCanApplyAsync(opportunityId);
        if (eligibility.TryGetError(out var error))
            return new ApplicationCheckoutError.Ineligible(error);

        return await checkout.CreateApplyCheckoutAsync(opportunityId);
    }

    public async Task<Result<Checkout, ApplicationCheckoutError>> AcceptCheckoutAsync(int applicationId)
    {
        var eligibility = await CheckCanAcceptAsync(applicationId);
        if (eligibility.TryGetError(out var error))
            return new ApplicationCheckoutError.Ineligible(error);

        return await checkout.CreateAcceptCheckoutAsync(applicationId);
    }

    public async Task<UnitResult<AcceptApplicationError>> AcceptAsync(
        int applicationId,
        string? paymentMethodId,
        ESignatureRequest eSignature,
        CancellationToken ct = default) =>
        await unitOfWork.ExecuteAsync(
            () => AcceptCoreAsync(applicationId, paymentMethodId, eSignature, ct),
            ct);

    private async Task<UnitResult<AcceptApplicationError>> AcceptCoreAsync(
        int applicationId,
        string? paymentMethodId,
        ESignatureRequest eSignature,
        CancellationToken ct)
    {
        var eligibility = await CheckCanAcceptAsync(applicationId);
        if (eligibility.TryGetError(out var error))
            return new AcceptApplicationError.Ineligible(error);

        var application = await repository.GetWithVerifyPaymentByIdAsync(applicationId, ct);
        if (application is null)
            return new AcceptApplicationError.Ineligible(
                new ApplicationEligibilityError.ApplicationNotFound());
        if (application.State != ApplicationState.Applied)
            return new AcceptApplicationError.InvalidState(application.State);

        var opportunityOption = await opportunities.GetDetailsAsync(application.OpportunityId, ct);
        if (!opportunityOption.TryGetValue(out var opportunity))
            return new AcceptApplicationError.Ineligible(
                new ApplicationEligibilityError.OpportunityNotFound());
        var dealOption = await deals.GetByIdAsync(opportunity.DealId, ct);
        if (!dealOption.TryGetValue(out var deal))
            return new AcceptApplicationError.Ineligible(
                new ApplicationEligibilityError.OpportunityNotFound());
        var artistOption = await artists.GetProfileAsync(application.ArtistId, ct);
        if (!artistOption.TryGetValue(out var artist))
            return new AcceptApplicationError.Ineligible(
                new ApplicationEligibilityError.ApplicationNotFound());
        var venueOption = await venues.GetProfileAsync(opportunity.VenueId, ct);
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
        var facts = new AcceptedApplicationFacts(
            operationId,
            application.Id,
            application.OpportunityId,
            application.ArtistId,
            opportunity.VenueId,
            application.VenueTenantId,
            application.ArtistTenantId,
            deal.PaymentMethod,
            opportunity.StartDate,
            opportunity.EndDate,
            opportunity.Genres,
            artist.Name,
            venue.Name,
            termsRenderer.Render(deal),
            legal.PlatformTermsVersion,
            application.ArtistESignature,
            new Signature(
                userId,
                timeProvider.GetUtcNow().UtcDateTime,
                clientContext.IpAddress,
                clientContext.UserAgent,
                eSignature.SignatoryName,
                eSignature.DrawnSignatureImage));
        var accepted = acceptFactory.Create(deal) switch
        {
            IStandardAccept method => method.Create(facts, application, deal),
            IPrepaidAccept method when !string.IsNullOrWhiteSpace(paymentMethodId) =>
                method.Create(facts, application, deal, paymentMethodId),
            IPrepaidAccept => new AcceptApplicationError.PaymentMethodRequired(),
            _ => throw new UnreachableException()
        };
        if (accepted.TryGetError(out var acceptanceError))
            return acceptanceError;
        if (!accepted.TryGetValue(out var acceptedApplication))
            throw new InvalidOperationException("Acceptance succeeded without an accepted application fact.");

        if (!await opportunities.TryClaimAsync(
                application.OpportunityId,
                application.VenueTenantId,
                ct))
            return new AcceptApplicationError.OpportunityUnavailable(application.OpportunityId);

        application.BeginAcceptance(operationId);
        application.Accept(acceptedApplication);
        application.NotifyCounterparty(ApplicationNotification.Accepted);
        await repository.SaveChangesAsync(ct);
        await repository.RejectAllExceptAsync(application.OpportunityId, application.Id, ct);
        await notifier.AcceptedAsync(applicationId);
        return new Success();
    }

    public async Task<UnitResult<WithdrawApplicationError>> WithdrawAsync(
        int applicationId,
        CancellationToken ct = default)
    {
        var application = await repository.GetByIdAsync(applicationId, ct);
        if (application is null)
            return new WithdrawApplicationError.ApplicationNotFound(applicationId);
        if (application.State != ApplicationState.Applied)
            return new WithdrawApplicationError.InvalidState(application.State);

        application.Withdraw();
        application.NotifyCounterparty(ApplicationNotification.Withdrawn);
        await repository.SaveChangesAsync(ct);
        await notifier.WithdrawnAsync(applicationId);
        return new Success();
    }

    public async Task<UnitResult<RejectApplicationError>> RejectAsync(int applicationId)
    {
        var application = await repository.GetByIdAsync(applicationId);
        if (application is null)
            return new RejectApplicationError.ApplicationNotFound(applicationId);
        if (application.State != ApplicationState.Applied)
            return new RejectApplicationError.InvalidState(application.State);

        application.Reject();
        application.NotifyCounterparty(ApplicationNotification.Rejected);
        await repository.SaveChangesAsync();
        await notifier.RejectedAsync(applicationId);
        return new Success();
    }

    private async Task<UnitResult<ApplicationEligibilityError>> CheckCanApplyAsync(int opportunityId)
    {
        var artistOption = await artists.GetCurrentProfileAsync();
        if (!artistOption.TryGetValue(out var artist))
            return new ApplicationEligibilityError.MissingArtist();

        var opportunityOption = await opportunities.GetOpenDetailsAsync(opportunityId);
        if (!opportunityOption.TryGetValue(out var opportunity))
            return new ApplicationEligibilityError.OpportunityNotFound();

        var validation = await validator.CanApplyAsync(opportunity, artist.Id);
        return validation.TryGetErrors(out var errors)
            ? new ApplicationEligibilityError.Invalid(new ValidationErrors(errors.ToDictionary()))
            : new Success();
    }

    private async Task<UnitResult<ApplicationEligibilityError>> CheckCanAcceptAsync(int applicationId)
    {
        var application = await repository.GetByIdAsync(applicationId);
        if (application is null)
            return new ApplicationEligibilityError.ApplicationNotFound();

        var opportunityOption = await opportunities.GetDetailsAsync(application.OpportunityId);
        if (!opportunityOption.TryGetValue(out var opportunity))
            return new ApplicationEligibilityError.OpportunityNotFound();

        var validation = await validator.CanAcceptAsync(opportunity, application);
        return validation.TryGetErrors(out var errors)
            ? new ApplicationEligibilityError.Invalid(new ValidationErrors(errors.ToDictionary()))
            : new Success();
    }
}
