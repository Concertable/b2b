using System.Diagnostics;
using Concertable.B2B.Application.Application.Errors;
using Concertable.B2B.Application.Application.Mappers;
using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Application.Domain.Events;
using Concertable.B2B.Application.Domain.Lifecycle;
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
    private readonly IApplicationRepository applicationRepository;
    private readonly IApplicationValidator validator;
    private readonly IApplicationNotifier notifier;
    private readonly IArtistModule artistModule;
    private readonly IOpportunityModule opportunityModule;
    private readonly IVenueModule venueModule;
    private readonly IDealModule dealModule;
    private readonly ITenantContext tenantContext;
    private readonly ICurrentUser currentUser;
    private readonly IClientContext clientContext;
    private readonly ITermsFingerprintCalculator termsFingerprint;
    private readonly IDealTermsRenderer termsRenderer;
    private readonly IApplicationCheckoutService checkout;
    private readonly IApplicationMapper mapper;
    private readonly TimeProvider timeProvider;
    private readonly LegalSettings legal;
    private readonly IUnitOfWorkBehavior unitOfWork;

    public ApplicationService(
        IApplicationRepository applicationRepository,
        IApplicationValidator validator,
        IApplicationNotifier notifier,
        IArtistModule artistModule,
        IOpportunityModule opportunityModule,
        IVenueModule venueModule,
        IDealModule dealModule,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IClientContext clientContext,
        ITermsFingerprintCalculator termsFingerprint,
        IDealTermsRenderer termsRenderer,
        IApplicationCheckoutService checkout,
        IApplicationMapper mapper,
        TimeProvider timeProvider,
        IOptions<LegalSettings> legal,
        IUnitOfWorkBehavior unitOfWork)
    {
        this.applicationRepository = applicationRepository;
        this.validator = validator;
        this.notifier = notifier;
        this.artistModule = artistModule;
        this.opportunityModule = opportunityModule;
        this.venueModule = venueModule;
        this.dealModule = dealModule;
        this.tenantContext = tenantContext;
        this.currentUser = currentUser;
        this.clientContext = clientContext;
        this.termsFingerprint = termsFingerprint;
        this.termsRenderer = termsRenderer;
        this.checkout = checkout;
        this.mapper = mapper;
        this.timeProvider = timeProvider;
        this.legal = legal.Value;
        this.unitOfWork = unitOfWork;
    }

    public Task<Result<ApplicationDto, ApplicationError>> GetByIdAsync(int id) =>
        applicationRepository.GetByIdAsync(id)
            .ToOption()
            .OrFailure(() => (ApplicationError)new ApplicationError.NotFound(id))
            .MapAsync(mapper.ToDtoAsync);

    public async Task<Result<IReadOnlyList<ApplicationDto>, ApplicationError>> GetByOpportunityIdAsync(int id)
    {
        var opportunityOption = await this.opportunityModule.GetAsync(id);
        if (!opportunityOption.TryGetValue(out var opportunity) ||
            opportunity.VenueTenantId != tenantContext.TenantId)
            return new ApplicationError.OpportunityForbidden(id);

        var applications = await applicationRepository.GetByOpportunityIdAsync(id);
        return new Success<IReadOnlyList<ApplicationDto>>(await mapper.ToDtosAsync(applications));
    }

    public async Task<Result<IReadOnlyList<ApplicationDto>, ApplicationError>> GetPendingForArtistAsync()
    {
        var artistOption = await this.artistModule.GetCurrentProfileAsync();
        if (!artistOption.TryGetValue(out var artist))
            return new ApplicationError.MissingArtist();

        var applications = await applicationRepository.GetByArtistTenantIdAndStateAsync(
            artist.TenantId,
            State.Applied);
        var dtos = await mapper.ToDtosAsync(applications);
        return new Success<IReadOnlyList<ApplicationDto>>(
            dtos.Where(application => application.Opportunity.StartDate > timeProvider.GetUtcNow())
                .ToList());
    }

    public async Task<Result<IReadOnlyList<ApplicationDto>, ApplicationError>> GetRecentDeniedForArtistAsync()
    {
        var artistOption = await this.artistModule.GetCurrentProfileAsync();
        if (!artistOption.TryGetValue(out var artist))
            return new ApplicationError.MissingArtist();

        var applications = await applicationRepository.GetByArtistTenantIdAndStateAsync(
            artist.TenantId,
            State.Rejected);
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
            State.Applied);
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
        ESignatureRequest eSignature) =>
        ApplyAsync(opportunityId, null, eSignature);

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
            () => AcceptApplicationAsync(applicationId, paymentMethodId, eSignature, ct),
            ct);

    private async Task<UnitResult<AcceptApplicationError>> AcceptApplicationAsync(
        int applicationId,
        string? paymentMethodId,
        ESignatureRequest eSignature,
        CancellationToken ct)
    {
        var application = await applicationRepository.GetByIdAsync(applicationId, ct);
        if (application is null)
            return new AcceptApplicationError.Ineligible(
                new ApplicationEligibilityError.ApplicationNotFound());
        var eligibility = await CheckCanAcceptAsync(application, ct);
        if (eligibility.TryGetError(out var error))
            return new AcceptApplicationError.Ineligible(error);
        if (application.ValidateAccept().TryGetError(out var acceptError))
            return new AcceptApplicationError.InvalidTransition(acceptError);

        var opportunityOption = await this.opportunityModule.GetAsync(application.OpportunityId, ct);
        if (!opportunityOption.TryGetValue(out var opportunity))
            return new AcceptApplicationError.Ineligible(
                new ApplicationEligibilityError.OpportunityNotFound());
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
        Result<AcceptedApplication, AcceptApplicationError> accepted = deal switch
        {
            FlatFeeDealDto flatFee => new FlatFeeAcceptedApplication(
                operationId, application.Id, application.OpportunityId, application.ArtistId, opportunity.VenueId,
                application.VenueTenantId, application.ArtistTenantId, deal.PaymentMethod, opportunity.StartDate,
                opportunity.EndDate, opportunity.Genres.ToList(), artist.Name, venue.Name, termsRenderer.Render(deal),
                legal.PlatformTermsVersion, application.ArtistESignature.ToDto(), venueSignature.ToDto(), flatFee.Fee),
            DoorSplitDealDto doorSplit when !string.IsNullOrWhiteSpace(paymentMethodId) => new DoorSplitAcceptedApplication(
                operationId, application.Id, application.OpportunityId, application.ArtistId, opportunity.VenueId,
                application.VenueTenantId, application.ArtistTenantId, deal.PaymentMethod, opportunity.StartDate,
                opportunity.EndDate, opportunity.Genres.ToList(), artist.Name, venue.Name, termsRenderer.Render(deal),
                legal.PlatformTermsVersion, application.ArtistESignature.ToDto(), venueSignature.ToDto(),
                doorSplit.ArtistDoorPercent, paymentMethodId, application.Verification?.ToVerifyPayment()),
            VersusDealDto versus when !string.IsNullOrWhiteSpace(paymentMethodId) => new VersusAcceptedApplication(
                operationId, application.Id, application.OpportunityId, application.ArtistId, opportunity.VenueId,
                application.VenueTenantId, application.ArtistTenantId, deal.PaymentMethod, opportunity.StartDate,
                opportunity.EndDate, opportunity.Genres.ToList(), artist.Name, venue.Name, termsRenderer.Render(deal),
                legal.PlatformTermsVersion, application.ArtistESignature.ToDto(), venueSignature.ToDto(),
                versus.Guarantee, versus.ArtistDoorPercent, paymentMethodId, application.Verification?.ToVerifyPayment()),
            VenueHireDealDto venueHire when application is PrepaidApplication prepaid => new VenueHireAcceptedApplication(
                operationId, application.Id, application.OpportunityId, application.ArtistId, opportunity.VenueId,
                application.VenueTenantId, application.ArtistTenantId, deal.PaymentMethod, opportunity.StartDate,
                opportunity.EndDate, opportunity.Genres.ToList(), artist.Name, venue.Name, termsRenderer.Render(deal),
                legal.PlatformTermsVersion, application.ArtistESignature.ToDto(), venueSignature.ToDto(),
                venueHire.HireFee, prepaid.PaymentMethodId),
            DoorSplitDealDto or VersusDealDto or VenueHireDealDto => new AcceptApplicationError.PaymentMethodRequired(),
            _ => throw new ArgumentOutOfRangeException(nameof(deal), deal, null)
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

    public async Task<UnitResult<WithdrawApplicationError>> WithdrawAsync(
        int applicationId,
        CancellationToken ct = default) =>
        await unitOfWork.ExecuteAsync(() => WithdrawCoreAsync(applicationId, ct), ct);

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
        if (!await applicationRepository.TrySaveChangesAsync(ct))
        {
            application = await applicationRepository.GetByIdAsync(applicationId, ct);
            return application?.State == State.Withdrawn
                ? new Success()
                : new WithdrawApplicationError.Superseded(applicationId);
        }

        await notifier.WithdrawnAsync(applicationId);
        return new Success();
    }

    public async Task<UnitResult<RejectApplicationError>> RejectAsync(
        int applicationId,
        CancellationToken ct = default) =>
        await unitOfWork.ExecuteAsync(() => RejectCoreAsync(applicationId, ct), ct);

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
        if (!await applicationRepository.TrySaveChangesAsync(ct))
        {
            application = await applicationRepository.GetByIdAsync(applicationId, ct);
            return application?.State == State.Rejected
                ? new Success()
                : new RejectApplicationError.Superseded(applicationId);
        }

        await notifier.RejectedAsync(applicationId);
        return new Success();
    }

    public async Task<UnitResult<CancelApplicationError>> CancelAsync(
        int applicationId,
        CancellationToken ct = default) =>
        await unitOfWork.ExecuteAsync(() => CancelCoreAsync(applicationId, ct), ct);

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
        if (!await applicationRepository.TrySaveChangesAsync(ct))
        {
            application = await applicationRepository.GetByIdAsync(applicationId, ct);
            return application?.State == State.Cancelled
                ? new Success()
                : new CancelApplicationError.Superseded(applicationId);
        }

        await notifier.CancelledAsync(applicationId);
        return new Success();
    }

    private async Task<UnitResult<ApplicationEligibilityError>> CheckCanApplyAsync(int opportunityId)
    {
        var artistOption = await this.artistModule.GetCurrentProfileAsync();
        if (!artistOption.TryGetValue(out var artist))
            return new ApplicationEligibilityError.MissingArtist();

        var opportunityOption = await this.opportunityModule.GetOpenAsync(opportunityId);
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
        var opportunityOption = await this.opportunityModule.GetAsync(application.OpportunityId, ct);
        if (!opportunityOption.TryGetValue(out var opportunity))
            return new ApplicationEligibilityError.OpportunityNotFound();

        var validation = await validator.CanAcceptAsync(opportunity, application);
        return validation.TryGetErrors(out var errors)
            ? new ApplicationEligibilityError.Invalid(new ValidationErrors(errors.ToDictionary()))
            : new Success();
    }
}
