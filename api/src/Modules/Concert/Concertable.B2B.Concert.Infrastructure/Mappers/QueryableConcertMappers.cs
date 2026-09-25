using Concertable.B2B.Artist.Domain.ReadModels;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.ReadModels;
using Concertable.B2B.Venue.Domain.ReadModels;

namespace Concertable.B2B.Concert.Infrastructure.Mappers;

internal static class QueryableConcertMappers
{
    extension(IQueryable<ConcertEntity> query)
    {
        public IQueryable<ConcertOperations> ToOperations(
            IQueryable<ConcertRatingProjection> concertRatings,
            IQueryable<ArtistRatingProjection> artistRatings,
            IQueryable<VenueRatingProjection> venueRatings) =>
            from c in query
            join cr in concertRatings on c.Id equals cr.ConcertId into crg
            from concertRating in crg.DefaultIfEmpty()
            join ar in artistRatings on c.ArtistId equals ar.ArtistId into arg
            from artistRating in arg.DefaultIfEmpty()
            join vr in venueRatings on c.VenueId equals vr.VenueId into vrg
            from venueRating in vrg.DefaultIfEmpty()
            select new ConcertOperations
            {
                Id = c.Id,
                ApplicationId = c.ApplicationId,
                Name = c.Name,
                About = c.About,
                BannerUrl = c.BannerUrl ?? c.Artist.BannerUrl,
                Avatar = c.Avatar ?? c.Artist.Avatar,
                Rating = (double?)concertRating.AverageRating ?? 0.0,
                TotalTickets = c.TotalTickets,
                AvailableTickets = 0,
                Price = c.Price,
                DatePosted = c.DatePosted,
                StartDate = c.Period.Start,
                EndDate = c.Period.End,
                State = c.State,
                Genres = c.Genres,
                Venue = new ConcertVenue
                {
                    Id = c.Venue.Id,
                    Name = c.Venue.Name,
                    Rating = (double?)venueRating.AverageRating ?? 0.0,
                    County = c.Venue.Address.County,
                    Town = c.Venue.Address.Town,
                    Latitude = c.Venue.Location.Y,
                    Longitude = c.Venue.Location.X
                },
                Artist = new ConcertArtist
                {
                    Id = c.Artist.Id,
                    Name = c.Artist.Name,
                    Avatar = c.Artist.Avatar,
                    County = c.Artist.Address.County,
                    Town = c.Artist.Address.Town,
                    Rating = (double?)artistRating.AverageRating ?? 0.0,
                    Genres = c.Artist.Genres.Select(g => g.Genre)
                }
            };

        public IQueryable<ConcertSummary> ToSummary() =>
            query.Select(c => new ConcertSummary(
                c.Id,
                c.Name,
                c.Period.Start,
                c.Period.End,
                c.Venue.Name,
                c.Artist.Name,
                c.State));

        public IQueryable<PublishedConcert> ToPublished() =>
            query.Select(c => new PublishedConcert(
                c.Id,
                c.Name,
                c.About,
                c.Period.Start,
                c.Period.End,
                c.Venue.Name,
                c.Artist.Name,
                c.Price));
    }
}
