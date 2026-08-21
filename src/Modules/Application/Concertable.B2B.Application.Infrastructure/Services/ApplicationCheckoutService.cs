using Concertable.B2B.Application.Application.Errors;
using Concertable.B2B.Application.Application.Responses;
using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Opportunity.Contracts;
using Concertable.B2B.Venue.Contracts;
using Concertable.Kernel.Exceptions;
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
    private readonly IManagerPaymentOperationsClient payment;
    private readonly ITenantContext tenantContext;

    public ApplicationCheckoutService(
        IApplicationRepository applications,
        IArtistModule artists,
        IOpportunityModule opportunities,
        IVenueModule venues,
        IDealModule deals,
        IManagerPaymentOperationsClient payment,
        ITenantContext tenantContext)
    {
        this.applications = applications;
        this.artists = artists;
        this.opportunities = opportunities;
        this.venues = venues;
        this.deals = deals;
        this.payment = payment;
        this.tenantContext = tenantContext;
    }

    public async Task<Result<Checkout, ApplicationEligibilityError>> CreateApplyCheckoutAsync(
        int opportunityId)
    {
        var opportunity = await GetOpportunityAsync(opportunityId);
        var deal = await GetDealAsync(opportunity.DealId);
        if (deal is not VenueHireDealDto venueHire)
            throw new BadRequestException("This deal does not support a pre-apply checkout");

        var venue = await GetVenueAsync(opportunity.VenueId);
        var artistTenantId = tenantContext.TenantId
            ?? throw new ForbiddenException("No tenant for current user");
        var metadata = new Dictionary<string, string>
        {
            [PaymentMetadataKeys.Type] = TransactionTypes.ApplicationApply,
            [PaymentMetadataKeys.OpportunityId] = opportunityId.ToString()
        };
        var session = await payment.CreateSetupSessionAsync(artistTenantId, metadata);
        return new Checkout(
            new FlatPayment(venueHire.HireFee),
            new PayeeSummary(venue.Name, venue.Email),
            session,
            CheckoutLabels.Charge);
    }

    public async Task<Checkout> CreateAcceptCheckoutAsync(int applicationId)
    {
        var application = await applications.GetByIdAsync(applicationId)
            .OrNotFound(Concertable.B2B.Application.Contracts.DisplayNames.Application);
        var opportunity = await GetOpportunityAsync(application.OpportunityId);
        var deal = await GetDealAsync(opportunity.DealId);
        var artist = await GetArtistAsync(application.ArtistId);
        var venue = await GetVenueAsync(opportunity.VenueId);
        var metadata = new Dictionary<string, string>
        {
            [PaymentMetadataKeys.ApplicationId] = applicationId.ToString()
        };

        if (deal is FlatFeeDealDto flatFee)
        {
            metadata[PaymentMetadataKeys.Type] = TransactionTypes.ApplicationAccept;
            var session = await payment.CreateHoldSessionAsync(
                application.VenueTenantId,
                Money.Gbp(flatFee.Fee),
                metadata);
            return new Checkout(
                new FlatPayment(flatFee.Fee),
                new PayeeSummary(artist.Name, artist.Email),
                session,
                CheckoutLabels.Charge);
        }

        metadata[PaymentMetadataKeys.Type] = TransactionTypes.Verify;
        metadata[PaymentMetadataKeys.VenueManagerId] = venue.UserId.ToString();
        var verification = await payment.CreateVerifySessionAsync(application.VenueTenantId, metadata);
        return new Checkout(
            ToPaymentAmount(deal),
            new PayeeSummary(artist.Name, artist.Email),
            verification,
            CheckoutLabels.Settlement);
    }

    private async Task<OpportunityDetails> GetOpportunityAsync(int opportunityId)
    {
        var option = await opportunities.GetDetailsAsync(opportunityId);
        if (option.TryGetValue(out var opportunity))
            return opportunity;
        throw new NotFoundException(Concertable.B2B.Opportunity.Contracts.DisplayNames.Opportunity);
    }

    private async Task<DealDto> GetDealAsync(int dealId)
    {
        var option = await deals.GetByIdAsync(dealId);
        if (option.TryGetValue(out var deal))
            return deal;
        throw new NotFoundException("deal");
    }

    private async Task<ArtistProfile> GetArtistAsync(int artistId)
    {
        var option = await artists.GetProfileAsync(artistId);
        if (option.TryGetValue(out var artist))
            return artist;
        throw new NotFoundException("artist");
    }

    private async Task<VenueProfile> GetVenueAsync(int venueId)
    {
        var option = await venues.GetProfileAsync(venueId);
        if (option.TryGetValue(out var venue))
            return venue;
        throw new NotFoundException("venue");
    }

    private static IPaymentAmount ToPaymentAmount(DealDto deal) => deal switch
    {
        DoorSplitDealDto doorSplit => new DoorSharePayment(doorSplit.ArtistDoorPercent),
        VersusDealDto versus => new GuaranteedDoorPayment(versus.Guarantee, versus.ArtistDoorPercent),
        _ => throw new BadRequestException("This deal does not support an accept checkout")
    };
}
