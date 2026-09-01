using Concertable.B2B.Application.Application.Mappers;
using Concertable.B2B.Application.Application.Strategies;
using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Opportunity.Contracts;
using Concertable.B2B.Venue.Contracts;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Application.Infrastructure.Strategies;

internal sealed class VersusAccept : IAcceptPaid
{
    private readonly IDealTermsRenderer dealTermsRenderer;
    private readonly LegalSettings legal;

    public VersusAccept(
        IDealTermsRenderer dealTermsRenderer,
        IOptions<LegalSettings> legal)
    {
        this.dealTermsRenderer = dealTermsRenderer;
        this.legal = legal.Value;
    }

    public Result<AcceptedApplication, AcceptApplicationError> Accept(
        ApplicationEntity application,
        OpportunityDto opportunity,
        ArtistProfile artist,
        VenueProfile venue,
        DealDto deal,
        Signature venueSignature,
        Guid operationId,
        string paymentMethodId)
    {
        if (deal is not VersusDealDto versus)
            throw new InvalidOperationException(
                $"{nameof(VersusAccept)} cannot accept {deal.GetType().Name}.");

        return new VersusAcceptedApplication(
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
            opportunity.Genres.ToList(),
            artist.Name,
            venue.Name,
            dealTermsRenderer.Render(deal),
            legal.PlatformTermsVersion,
            application.ArtistESignature.ToDto(),
            venueSignature.ToDto(),
            versus.Guarantee,
            versus.ArtistDoorPercent,
            paymentMethodId,
            application.Verification?.ToVerifyPayment());
    }
}
