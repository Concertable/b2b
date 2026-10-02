using Concertable.B2B.Application.Application.Errors;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Tenant.Contracts;
using ITenantResolver = Concertable.B2B.Tenant.Contracts.ITenantResolver;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Application.Application.Responses;
using Concertable.B2B.Application.Application.Strategies;
using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Infrastructure.Payments;
using Concertable.B2B.Opportunity.Contracts;
using Concertable.B2B.Venue.Contracts;
using Concertable.Kernel.ValueObjects;
using Concertable.Payment.Contracts;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Application.Infrastructure.Services;

internal sealed class ApplicationCheckoutService : IApplicationCheckoutService
{
    private readonly IApplicationRepository repository;
    private readonly IApplicationPrivilegedRepository privilegedRepository;
    private readonly IMembershipContext membership;
    private readonly ICommandAuthorizationContext commandAuthorization;
    private readonly ITenantResolver tenantResolver;
    private readonly ITransactionRunner transactionRunner;
    private readonly IPermissionAuthorization permissions;
    private readonly IResourceAuthorization resources;
    private readonly IArtistModule artistModule;
    private readonly IOpportunityModule opportunityModule;
    private readonly IVenueModule venueModule;
    private readonly IDealModule dealModule;
    private readonly IPaymentSessionOperationsClient paymentSessions;
    private readonly IEscrowOperationsClient escrowOperationsClient;
    private readonly IDealStrategyFactory<ICommitmentReferenceStep> commitmentFactory;
    private readonly ITenantContext tenantContext;
    private readonly LegalSettings legal;

    public ApplicationCheckoutService(
        IApplicationRepository repository,
        IApplicationPrivilegedRepository privilegedRepository,
        IMembershipContext membership,
        ICommandAuthorizationContext commandAuthorization,
        ITenantResolver tenantResolver,
        ITransactionRunner transactionRunner,
        IPermissionAuthorization permissions,
        IResourceAuthorization resources,
        IArtistModule artistModule,
        IOpportunityModule opportunityModule,
        IVenueModule venueModule,
        IDealModule dealModule,
        IPaymentSessionOperationsClient paymentSessions,
        IEscrowOperationsClient escrowOperationsClient,
        IDealStrategyFactory<ICommitmentReferenceStep> commitmentFactory,
        ITenantContext tenantContext,
        IOptions<LegalSettings> legal)
    {
        this.repository = repository;
        this.privilegedRepository = privilegedRepository;
        this.membership = membership;
        this.commandAuthorization = commandAuthorization;
        this.tenantResolver = tenantResolver;
        this.transactionRunner = transactionRunner;
        this.permissions = permissions;
        this.resources = resources;
        this.artistModule = artistModule;
        this.opportunityModule = opportunityModule;
        this.venueModule = venueModule;
        this.dealModule = dealModule;
        this.paymentSessions = paymentSessions;
        this.escrowOperationsClient = escrowOperationsClient;
        this.commitmentFactory = commitmentFactory;
        this.tenantContext = tenantContext;
        this.legal = legal.Value;
    }

    public async Task<Result<Checkout, ApplicationCheckoutError>> CreateApplyCheckoutAsync(
        int opportunityId)
    {
        if (await permissions.CheckAsync(TenantPermission.ApplicationsSubmit, ResourceAudience.TenantResources)
            != AuthorizationDecision.Allowed)
            return new ApplicationCheckoutError.Ineligible(
                new ApplicationEligibilityError.MissingArtist());
        var opportunityOption = await opportunityModule.GetOpenAsync(opportunityId);
        if (!opportunityOption.TryGetValue(out var opportunity))
            return new ApplicationCheckoutError.OpportunityNotFound();

        var dealOption = await dealModule.GetByIdAsync(opportunity.DealId);
        if (!dealOption.TryGetValue(out var deal))
            return new ApplicationCheckoutError.DealNotFound();
        if (deal is not VenueHireDealDto venueHire)
            return new ApplicationCheckoutError.ApplyCheckoutUnsupported(deal.DealType);

        var venueOption = await venueModule.GetProfileAsync(opportunity.VenueId);
        if (!venueOption.TryGetValue(out var venue))
            return new ApplicationCheckoutError.VenueNotFound();
        if (tenantContext.TenantId is not { } artistTenantId)
            return new ApplicationCheckoutError.MissingTenant();

        var setup = await paymentSessions.SetupPaymentMethodAsync(
            new PaymentMethodSetupRequest(
                PaymentOperationReferences.MethodSetup(opportunityId, artistTenantId),
                PaymentSessionKind.PaymentMethodSetup,
                artistTenantId,
                legal.MandateTermsVersion));
        if (!setup.TryGetValue(out var session))
        {
            setup.TryGetError(out var setupError);
            return new ApplicationCheckoutError.PaymentSessionUnavailable(setupError!);
        }

        return new Checkout(
            new FlatPayment(venueHire.HireFee),
            new PayeeSummary(venue.Name, venue.Email),
            new CheckoutSession(session.ClientSecret, session.CustomerSessionSecret, session.CustomerToken),
            CheckoutLabels.Charge);
    }

    public async Task<Result<Checkout, ApplicationCheckoutError>> CreateAcceptCheckoutAsync(int applicationId)
    {
        if (applicationId <= 0 || await resources.CheckAsync(new AuthorizationRequest(
            TenantPermission.ApplicationsDecide,
            ResourceAddress.Create(ResourceKind.Application, applicationId),
            ResourceFacet.Proposal)) != AuthorizationDecision.Allowed)
            return new ApplicationCheckoutError.ApplicationNotFound();
        var application = await repository.GetDecisionByIdAsync(applicationId);
        if (application is null)
            return new ApplicationCheckoutError.ApplicationNotFound();

        var opportunityOption = await opportunityModule.GetAsync(application.OpportunityId);
        if (!opportunityOption.TryGetValue(out var opportunity))
            return new ApplicationCheckoutError.OpportunityNotFound();

        var dealOption = await dealModule.GetByIdAsync(opportunity.DealId);
        if (!dealOption.TryGetValue(out var deal))
            return new ApplicationCheckoutError.DealNotFound();

        var artistOption = await artistModule.GetProfileAsync(application.ArtistId);
        if (!artistOption.TryGetValue(out var artist))
            return new ApplicationCheckoutError.ArtistNotFound();

        if ((await venueModule.GetProfileAsync(opportunity.VenueId)).IsNone)
            return new ApplicationCheckoutError.VenueNotFound();

        if (deal is FlatFeeDealDto flatFee)
        {
            var prepared = await PrepareAcceptCheckoutAsync(applicationId);
            if (!prepared.TryGetValue(out var commitment))
            {
                prepared.TryGetError(out var prepareError);
                return prepareError!;
            }

            var authorization = await escrowOperationsClient.AuthorizeAsync(
                commitment.OperationId,
                commitment.Reference,
                commitment.VenueTenantId,
                commitment.ArtistTenantId,
                Money.Gbp(flatFee.Fee));
            if (!authorization.TryGetValue(out var hold))
            {
                authorization.TryGetError(out var authorizationError);
                return new ApplicationCheckoutError.PaymentSessionUnavailable(authorizationError!);
            }

            return new Checkout(
                new FlatPayment(flatFee.Fee),
                new PayeeSummary(artist.Name, artist.Email),
                new CheckoutSession(hold.ClientSecret, hold.CustomerSessionSecret, hold.CustomerToken),
                CheckoutLabels.Charge);
        }

        if (deal is not (DoorSplitDealDto or VersusDealDto))
            return new ApplicationCheckoutError.AcceptCheckoutUnsupported(deal.DealType);

        var verification = await paymentSessions.SetupPaymentMethodAsync(
            new PaymentMethodSetupRequest(
                commitmentFactory.Create(deal.DealType).Resolve(application),
                PaymentSessionKind.PaymentMethodVerification,
                application.VenueTenantId,
                legal.MandateTermsVersion));
        if (!verification.TryGetValue(out var verified))
        {
            verification.TryGetError(out var verificationError);
            return new ApplicationCheckoutError.PaymentSessionUnavailable(verificationError!);
        }

        return new Checkout(
            ToPaymentAmount(deal),
            new PayeeSummary(artist.Name, artist.Email),
            new CheckoutSession(verified.ClientSecret, verified.CustomerSessionSecret, verified.CustomerToken),
            CheckoutLabels.Settlement);
    }

    private Task<Result<AcceptCheckoutCommitment, ApplicationCheckoutError>> PrepareAcceptCheckoutAsync(
        int applicationId)
    {
        if (membership.Membership is not { } actor)
            return Task.FromResult<Result<AcceptCheckoutCommitment, ApplicationCheckoutError>>(
                new ApplicationCheckoutError.ApplicationNotFound());
        return transactionRunner.ExecuteAsync<ApplicationCheckoutService,
            Result<AcceptCheckoutCommitment, ApplicationCheckoutError>>(
            (service, ct) => service.PrepareAcceptCheckoutCoreAsync(applicationId, actor, ct));
    }

    private async Task<Result<AcceptCheckoutCommitment, ApplicationCheckoutError>>
        PrepareAcceptCheckoutCoreAsync(int applicationId, MembershipSnapshot expectedActor, CancellationToken ct)
    {
        commandAuthorization.RegisterFailure<Result<AcceptCheckoutCommitment, ApplicationCheckoutError>>(
            () => new ApplicationCheckoutError.ApplicationNotFound());
        var opportunityId = await privilegedRepository.GetOpportunityIdAsync(applicationId, ct);
        var parties = await privilegedRepository.GetNotificationTenantIdsAsync(applicationId, false, ct);
        if (opportunityId is null || parties.Count == 0)
            return new ApplicationCheckoutError.ApplicationNotFound();
        var resolutionOption = await tenantResolver.ResolveManyAsync(expectedActor, parties, ct);
        if (!resolutionOption.TryGetValue(out var resolution)
            || !resolution.ExistingTenantIds.SetEquals(parties))
            return new ApplicationCheckoutError.ApplicationNotFound();
        await privilegedRepository.LockOpportunityAsync(opportunityId.Value, ct);
        if (await resources.RequireAsync(new AuthorizationRequest(
            TenantPermission.ApplicationsDecide,
            ResourceAddress.Create(ResourceKind.Application, applicationId),
            ResourceFacet.Proposal), ct) != AuthorizationDecision.Allowed)
            return new ApplicationCheckoutError.ApplicationNotFound();
        var application = await privilegedRepository.GetByIdForUpdateAsync(applicationId, ct);
        if (application is null || application.VenueTenantId != resolution.Actor.TenantId
            || !parties.ToHashSet().SetEquals([application.VenueTenantId, application.ArtistTenantId]))
            return new ApplicationCheckoutError.ApplicationNotFound();
        var operationId = application.BeginAcceptance();
        return new AcceptCheckoutCommitment(
            operationId, commitmentFactory.Create(application.DealType).Resolve(application),
            application.VenueTenantId, application.ArtistTenantId);
    }

    private sealed record AcceptCheckoutCommitment(
        Guid OperationId, PaymentOperationReference Reference,
        Guid VenueTenantId, Guid ArtistTenantId);

    private static IPaymentAmount ToPaymentAmount(DealDto deal) => deal switch
    {
        DoorSplitDealDto doorSplit => new DoorSharePayment(doorSplit.ArtistDoorPercent),
        VersusDealDto versus => new GuaranteedDoorPayment(versus.Guarantee, versus.ArtistDoorPercent),
        _ => throw new InvalidOperationException()
    };
}
