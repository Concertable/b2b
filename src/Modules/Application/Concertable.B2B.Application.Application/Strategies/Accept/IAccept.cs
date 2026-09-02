using Concertable.B2B.Application.Application.Errors;
using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Application.Domain.ValueObjects;
using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Opportunity.Contracts;
using Concertable.B2B.Venue.Contracts;

namespace Concertable.B2B.Application.Application.Strategies;

internal interface IAccept : IDealStrategy
{
    Result<AcceptedApplication, AcceptApplicationError> Accept(
        ApplicationEntity application,
        OpportunityDto opportunity,
        ArtistDto artist,
        VenueDto venue,
        DealDto deal,
        Signature venueSignature,
        Guid operationId);
}
