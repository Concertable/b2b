using Concertable.B2B.Artist.Domain.ReadModels;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.ReadModels;
using Concertable.B2B.Venue.Domain.ReadModels;

namespace Concertable.B2B.Concert.Infrastructure.Data;

internal interface IConcertReadDbContext
{
    IQueryable<ConcertEntity> Concerts { get; }
    IQueryable<OpportunityEntity> Opportunities { get; }
    IQueryable<SelfBillingAgreementEntity> SelfBillingAgreements { get; }
    IQueryable<ConcertRatingProjection> ConcertRatingProjections { get; }
    IQueryable<ArtistRatingProjection> ArtistRatingProjections { get; }
    IQueryable<VenueRatingProjection> VenueRatingProjections { get; }
}
