using Concertable.Concert.Application.DTOs;

namespace Concertable.Concert.Application.Mappers;

internal static class ConcertMappers
{
    private static ConcertArtistDto ToConcertArtistDto(this ArtistReadModel artist) => new()
    {
        Id = artist.Id,
        Name = artist.Name,
        Avatar = artist.Avatar,
        County = artist.County ?? string.Empty,
        Town = artist.Town ?? string.Empty,
        Genres = artist.Genres.Select(ag => ag.Genre)
    };

    private static ConcertVenueDto ToConcertVenueDto(this VenueReadModel venue) => new()
    {
        Id = venue.Id,
        Name = venue.Name,
        County = venue.County ?? string.Empty,
        Town = venue.Town ?? string.Empty,
        Latitude = venue.Location?.Y ?? 0.0,
        Longitude = venue.Location?.X ?? 0.0
    };

    public static ConcertDto ToDto(this ConcertEntity concert) => new()
    {
        Id = concert.Id,
        Name = concert.Name,
        About = concert.About,
        BannerUrl = concert.BannerUrl ?? concert.Booking.Application.Artist.BannerUrl,
        Avatar = concert.Avatar ?? concert.Booking.Application.Artist.Avatar,
        Price = concert.Price,
        TotalTickets = concert.TotalTickets,
        AvailableTickets = 0, // moved to Customer.Concert.ConcertEntity (sub-step #11); UI reads from Search projection in end-state
        DatePosted = concert.DatePosted,
        StartDate = concert.Booking.Application.Opportunity.Period.Start,
        EndDate = concert.Booking.Application.Opportunity.Period.End,
        Venue = concert.Booking.Application.Opportunity.Venue.ToConcertVenueDto(),
        Artist = concert.Booking.Application.Artist.ToConcertArtistDto(),
        Genres = concert.Genres
    };

    public static ConcertSnapshot ToSnapshotDto(this ConcertEntity concert) => new()
    {
        Id = concert.Id,
        Name = concert.Name,
        ImageUrl = concert.Booking.Application.Artist.Avatar,
        StartDate = concert.Booking.Application.Opportunity.Period.Start,
        EndDate = concert.Booking.Application.Opportunity.Period.End,
        County = concert.Booking.Application.Opportunity.Venue.County ?? string.Empty,
        Town = concert.Booking.Application.Opportunity.Venue.Town ?? string.Empty,
        DatePosted = concert.DatePosted
    };

    public static IEnumerable<ConcertDto> ToDtos(this IEnumerable<ConcertEntity> concerts) =>
        concerts.Select(e => e.ToDto());
}
