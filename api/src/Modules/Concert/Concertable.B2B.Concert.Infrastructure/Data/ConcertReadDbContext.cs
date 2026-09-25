using Concertable.B2B.Artist.Domain.ReadModels;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.ReadModels;
using Concertable.B2B.Venue.Domain.ReadModels;
using Concertable.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Data;

internal sealed class ConcertReadDbContext(
    DbContextOptions<ConcertReadDbContext> options,
    ConcertConfigurationProvider provider)
    : ReadDbContext(options, provider, Schema.Name), IConcertReadDbContext
{
    IQueryable<ConcertEntity> IConcertReadDbContext.Concerts => Query<ConcertEntity>();
    IQueryable<SelfBillingAgreementEntity> IConcertReadDbContext.SelfBillingAgreements =>
        Query<SelfBillingAgreementEntity>();
    IQueryable<ConcertRatingProjection> IConcertReadDbContext.ConcertRatingProjections =>
        Query<ConcertRatingProjection>();
    IQueryable<ArtistRatingProjection> IConcertReadDbContext.ArtistRatingProjections =>
        Query<ArtistRatingProjection>();
    IQueryable<VenueRatingProjection> IConcertReadDbContext.VenueRatingProjections =>
        Query<VenueRatingProjection>();
}
