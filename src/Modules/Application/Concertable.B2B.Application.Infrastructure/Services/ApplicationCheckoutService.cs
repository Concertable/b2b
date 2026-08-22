using Concertable.B2B.Application.Application.Errors;
using Concertable.B2B.Application.Application.Responses;
using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Opportunity.Contracts;
using Concertable.B2B.Venue.Contracts;
using Concertable.Kernel.ValueObjects;
using Concertable.Payment.Contracts;

namespace Concertable.B2B.Application.Infrastructure.Services;

internal sealed class ApplicationCheckoutService : IApplicationCheckoutService
{
    private readonly IApplicationRepository applications;
    private readonly IArtistModule artists;
    private readonly IOpportunityModule opportunities;
    private readonly IVenueModule venues;
    private readonly IDealModule deals;
    private readonly IManagerPaymentOperationsClient managerPaymentClient;
    private readonly ITenantContext tenantContext;

    public ApplicationCheckoutService(
        IApplicationRepository applications,
        IArtistModule artists,
        IOpportunityModule opportunities,
        IVenueModule venues,
        IDealModule deals,
        IManagerPaymentOperationsClient managerPaymentClient,
        ITenantContext tenantContext)
    {
        this.applications = applications;
        this.artists = artists;
        this.opportunities = opportunities;
        this.venues = venues;
        this.deals = deals;
        this.managerPaymentClient = managerPaymentClient;
        this.tenantContext = tenantContext;
    }

    public async Task<Result<Checkout, ApplicationCheckoutError>> CreateApplyCheckoutAsync(
        int opportunityId)
    {
        var opportunityOption = await opportunities.GetDetailsAsync(opportunityId);
        if (!opportunityOption.TryGetValue(out var opportunity))
            return new ApplicationCheckoutError.OpportunityNotFound();

        var dealOption = await deals.GetByIdAsync(opportunity.DealId);
        if (!dealOption.TryGetValue(out var deal))
            return new ApplicationCheckoutError.DealNotFound();
        if (deal is not VenueHireDealDto venueHire)
            return new ApplicationCheckoutError.ApplyCheckoutUnsupported(deal.DealType);

        var venueOption = await venues.GetProfileAsync(opportunity.VenueId);
        if (!venueOption.TryGetValue(out var venue))
            return new ApplicationCheckoutError.VenueNotFound();
        if (tenantContext.TenantId is not { } artistTenantId)
            return new ApplicationCheckoutError.MissingTenant();
        var metadata = new Dictionary<string, string>
        {
            [PaymentMetadataKeys.Type] = TransactionTypes.ApplicationApply,
            [PaymentMetadataKeys.OpportunityId] = opportunityId.ToString()
        };
        var session = await managerPaymentClient.CreateSetupSessionAsync(artistTenantId, metadata);
        return new Checkout(
            new FlatPayment(venueHire.HireFee),
            new PayeeSummary(venue.Name, venue.Email),
            session,
            CheckoutLabels.Charge);
    }

    public async Task<Result<Checkout, ApplicationCheckoutError>> CreateAcceptCheckoutAsync(int applicationId)
    {
        var application = await applications.GetByIdAsync(applicationId);
        if (application is null)
            return new ApplicationCheckoutError.ApplicationNotFound();

        var opportunityOption = await opportunities.GetDetailsAsync(application.OpportunityId);
        if (!opportunityOption.TryGetValue(out var opportunity))
            return new ApplicationCheckoutError.OpportunityNotFound();

        var dealOption = await deals.GetByIdAsync(opportunity.DealId);
        if (!dealOption.TryGetValue(out var deal))
            return new ApplicationCheckoutError.DealNotFound();

        var artistOption = await artists.GetProfileAsync(application.ArtistId);
        if (!artistOption.TryGetValue(out var artist))
            return new ApplicationCheckoutError.ArtistNotFound();

        var venueOption = await venues.GetProfileAsync(opportunity.VenueId);
        if (!venueOption.TryGetValue(out var venue))
            return new ApplicationCheckoutError.VenueNotFound();
        var metadata = new Dictionary<string, string>
        {
            [PaymentMetadataKeys.ApplicationId] = applicationId.ToString()
        };

        if (deal is FlatFeeDealDto flatFee)
        {
            metadata[PaymentMetadataKeys.Type] = TransactionTypes.ApplicationAccept;
            var session = await managerPaymentClient.CreateHoldSessionAsync(
                application.VenueTenantId,
                Money.Gbp(flatFee.Fee),
                metadata);
            return new Checkout(
                new FlatPayment(flatFee.Fee),
                new PayeeSummary(artist.Name, artist.Email),
                session,
                CheckoutLabels.Charge);
        }

        if (deal is not (DoorSplitDealDto or VersusDealDto))
            return new ApplicationCheckoutError.AcceptCheckoutUnsupported(deal.DealType);

        metadata[PaymentMetadataKeys.Type] = TransactionTypes.Verify;
        metadata[PaymentMetadataKeys.VenueManagerId] = venue.UserId.ToString();
        var verification = await managerPaymentClient.CreateVerifySessionAsync(application.VenueTenantId, metadata);
        return new Checkout(
            ToPaymentAmount(deal),
            new PayeeSummary(artist.Name, artist.Email),
            verification,
            CheckoutLabels.Settlement);
    }

    private static IPaymentAmount ToPaymentAmount(DealDto deal) => deal switch
    {
        DoorSplitDealDto doorSplit => new DoorSharePayment(doorSplit.ArtistDoorPercent),
        VersusDealDto versus => new GuaranteedDoorPayment(versus.Guarantee, versus.ArtistDoorPercent),
        _ => throw new InvalidOperationException()
    };
}
