using Concertable.B2B.Application.Application.Mappers;
using Concertable.B2B.Application.Application.Strategies;
using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Opportunity.Contracts;
using Concertable.B2B.Venue.Contracts;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Application.Infrastructure.Strategies;

internal sealed class PrepaidAccept : IAccept
{
    private readonly IDealTermsRenderer termsRenderer;
    private readonly LegalSettings legal;

    public PrepaidAccept(
        IDealTermsRenderer termsRenderer,
        IOptions<LegalSettings> legal)
    {
        this.termsRenderer = termsRenderer;
        this.legal = legal.Value;
    }

    public Result<AcceptedApplication, AcceptApplicationError> Accept(
        ApplicationEntity application,
        OpportunityDto opportunity,
        ArtistProfile artist,
        VenueProfile venue,
        DealDto deal,
        Signature venueSignature,
        Guid operationId)
    {
        if (deal is not VenueHireDealDto venueHire)
            throw new InvalidOperationException(
                $"{nameof(PrepaidAccept)} cannot accept {deal.GetType().Name}.");
        if (application is not PrepaidApplication prepaid)
            return new AcceptApplicationError.PaymentMethodRequired();

        return new VenueHireAcceptedApplication(
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
            this.termsRenderer.Render(deal),
            this.legal.PlatformTermsVersion,
            application.ArtistESignature.ToDto(),
            venueSignature.ToDto(),
            venueHire.HireFee,
            prepaid.PaymentMethodId);
    }
}
