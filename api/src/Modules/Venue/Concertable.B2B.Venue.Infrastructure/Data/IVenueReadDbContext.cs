namespace Concertable.B2B.Venue.Infrastructure.Data;

internal interface IVenueReadDbContext
{
    IQueryable<VenueEntity> Venues { get; }
    IQueryable<VenueRatingProjection> VenueRatingProjections { get; }
}
