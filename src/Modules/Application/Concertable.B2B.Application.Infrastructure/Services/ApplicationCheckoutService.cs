using Concertable.B2B.Application.Application.Errors;
using Concertable.B2B.Application.Application.Responses;
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
    private readonly IArtistModule artistModule;
    private readonly IOpportunityModule opportunityModule;
    private readonly IVenueModule venueModule;
    private readonly IDealModule dealModule;
    private readonly IPaymentSessionOperationsClient paymentSessions;
    private readonly ITenantContext tenantContext;
    private readonly LegalSettings legal;

    public ApplicationCheckoutService(
        IApplicationRepository repository,
        IArtistModule artistModule,
        IOpportunityModule opportunityModule,
        IVenueModule venueModule,
        IDealModule dealModule,
        IPaymentSessionOperationsClient paymentSessions,
        ITenantContext tenantContext,
        IOptions<LegalSettings> legal)
    {
        this.repository = repository;
        this.artistModule = artistModule;
        this.opportunityModule = opportunityModule;
        this.venueModule = venueModule;
        this.dealModule = dealModule;
        this.paymentSessions = paymentSessions;
        this.tenantContext = tenantContext;
        this.legal = legal.Value;
    }

    public async Task<Result<Checkout, ApplicationCheckoutError>> CreateApplyCheckoutAsync(
        int opportunityId)
    {
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
        var application = await repository.GetByIdAsync(applicationId);
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
            var authorization = await paymentSessions.CreateAsync(
                new PaymentSessionOperationRequest(
                    Guid.CreateVersion7(),
                    PaymentSessionKind.Authorization,
                    PaymentSession.OnSession,
                    PaymentOperationReferences.EscrowHold(applicationId),
                    application.VenueTenantId,
                    application.ArtistTenantId,
                    Money.Gbp(flatFee.Fee).ToMinorUnits(),
                    Currency.Gbp,
                    PaymentSessionFundsRouting.Destination));
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
                PaymentOperationReferences.MethodVerification(applicationId),
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

    private static IPaymentAmount ToPaymentAmount(DealDto deal) => deal switch
    {
        DoorSplitDealDto doorSplit => new DoorSharePayment(doorSplit.ArtistDoorPercent),
        VersusDealDto versus => new GuaranteedDoorPayment(versus.Guarantee, versus.ArtistDoorPercent),
        _ => throw new InvalidOperationException()
    };
}
