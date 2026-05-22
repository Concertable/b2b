using Concertable.Artist.Domain;
using Concertable.Venue.Domain;

namespace Concertable.Concert.Infrastructure.Mappers;

internal static class QueryableConcertMappers
{
    public static IQueryable<ConcertDto> ToDto(
        this IQueryable<ConcertEntity> query,
        IQueryable<ConcertRatingProjection> concertRatings,
        IQueryable<ArtistRatingProjection> artistRatings,
        IQueryable<VenueRatingProjection> venueRatings) =>
        from c in query.Where(c => c.Booking.Application.Opportunity.Venue.Location != null)
        join cr in concertRatings on c.Id equals cr.ConcertId into crg
        from concertRating in crg.DefaultIfEmpty()
        join ar in artistRatings on c.Booking.Application.ArtistId equals ar.ArtistId into arg
        from artistRating in arg.DefaultIfEmpty()
        join vr in venueRatings on c.Booking.Application.Opportunity.VenueId equals vr.VenueId into vrg
        from venueRating in vrg.DefaultIfEmpty()
        select new ConcertDto
        {
            Id = c.Id,
            Name = c.Name,
            About = c.About,
            BannerUrl = c.BannerUrl ?? c.Booking.Application.Artist.BannerUrl,
            Avatar = c.Avatar ?? c.Booking.Application.Artist.Avatar,
            Rating = (double?)concertRating.AverageRating ?? 0.0,
            Price = c.Price,
            TotalTickets = c.TotalTickets,
            AvailableTickets = 0,
            DatePosted = c.DatePosted,
            StartDate = c.Booking.Application.Opportunity.Period.Start,
            EndDate = c.Booking.Application.Opportunity.Period.End,
            Genres = c.Genres,
            Venue = new ConcertVenueDto
            {
                Id = c.Booking.Application.Opportunity.Venue.Id,
                Name = c.Booking.Application.Opportunity.Venue.Name,
                Rating = (double?)venueRating.AverageRating ?? 0.0,
                County = c.Booking.Application.Opportunity.Venue.County ?? string.Empty,
                Town = c.Booking.Application.Opportunity.Venue.Town ?? string.Empty,
                Latitude = c.Booking.Application.Opportunity.Venue.Location!.Y,
                Longitude = c.Booking.Application.Opportunity.Venue.Location!.X
            },
            Artist = new ConcertArtistDto
            {
                Id = c.Booking.Application.Artist.Id,
                Name = c.Booking.Application.Artist.Name,
                Avatar = c.Booking.Application.Artist.Avatar,
                County = c.Booking.Application.Artist.County ?? string.Empty,
                Town = c.Booking.Application.Artist.Town ?? string.Empty,
                Rating = (double?)artistRating.AverageRating ?? 0.0,
                Genres = c.Booking.Application.Artist.Genres.Select(g => g.Genre)
            }
        };

    public static IQueryable<ConcertSummaryDto> ToSummaryDto(
        this IQueryable<ConcertEntity> query,
        IQueryable<ArtistRatingProjection> artistRatings,
        IQueryable<VenueRatingProjection> venueRatings) =>
        from c in query.Where(c => c.Booking.Application.Opportunity.Venue.Location != null)
        join ar in artistRatings on c.Booking.Application.ArtistId equals ar.ArtistId into arg
        from artistRating in arg.DefaultIfEmpty()
        join vr in venueRatings on c.Booking.Application.Opportunity.VenueId equals vr.VenueId into vrg
        from venueRating in vrg.DefaultIfEmpty()
        select new ConcertSummaryDto
        {
            Id = c.Id,
            Name = c.Name,
            ImageUrl = c.Avatar ?? c.Booking.Application.Artist.Avatar,
            Price = c.Price,
            TotalTickets = c.TotalTickets,
            AvailableTickets = 0,
            DatePosted = c.DatePosted,
            StartDate = c.Booking.Application.Opportunity.Period.Start,
            EndDate = c.Booking.Application.Opportunity.Period.End,
            Venue = new ConcertVenueSummaryDto
            {
                Id = c.Booking.Application.Opportunity.Venue.Id,
                Name = c.Booking.Application.Opportunity.Venue.Name,
                Rating = (double?)venueRating.AverageRating ?? 0.0
            },
            Artist = new ConcertArtistSummaryDto
            {
                Id = c.Booking.Application.Artist.Id,
                Name = c.Booking.Application.Artist.Name,
                Rating = (double?)artistRating.AverageRating ?? 0.0,
                Genres = c.Booking.Application.Artist.Genres.Select(g => g.Genre)
            }
        };
}
