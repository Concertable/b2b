using Concertable.Concert.Api.Responses;

namespace Concertable.Concert.Api.Mappers;

internal static class ConcertResponseMappers
{
    public static ConcertSummaryResponse ToSummaryResponse(this ConcertSummaryDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        ImageUrl = dto.ImageUrl,
        Price = dto.Price,
        TotalTickets = dto.TotalTickets,
        AvailableTickets = dto.AvailableTickets,
        StartDate = dto.StartDate,
        EndDate = dto.EndDate,
        DatePosted = dto.DatePosted,
        Venue = new ConcertVenueSummaryResponse
        {
            Id = dto.Venue.Id,
            Name = dto.Venue.Name,
            Rating = dto.Venue.Rating
        },
        Artist = new ConcertArtistSummaryResponse
        {
            Id = dto.Artist.Id,
            Name = dto.Artist.Name,
            Rating = dto.Artist.Rating,
            Genres = dto.Artist.Genres.ToList()
        }
    };

    public static IEnumerable<ConcertSummaryResponse> ToSummaryResponses(this IEnumerable<ConcertSummaryDto> dtos) =>
        dtos.Select(d => d.ToSummaryResponse());

    public static ConcertDetailsResponse ToDetailsResponse(this ConcertDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        About = dto.About,
        BannerUrl = dto.BannerUrl ?? string.Empty,
        Avatar = dto.Avatar ?? dto.Artist.Avatar ?? string.Empty,
        Rating = dto.Rating,
        Price = dto.Price,
        TotalTickets = dto.TotalTickets,
        AvailableTickets = dto.AvailableTickets,
        StartDate = dto.StartDate,
        EndDate = dto.EndDate,
        DatePosted = dto.DatePosted,
        Genres = dto.Genres.ToList(),
        Artist = new ConcertArtistResponse
        {
            Id = dto.Artist.Id,
            Name = dto.Artist.Name,
            Avatar = dto.Artist.Avatar,
            Rating = dto.Artist.Rating,
            County = dto.Artist.County,
            Town = dto.Artist.Town,
            Genres = dto.Artist.Genres.ToList()
        },
        Venue = new ConcertVenueResponse
        {
            Id = dto.Venue.Id,
            Name = dto.Venue.Name,
            County = dto.Venue.County,
            Town = dto.Venue.Town,
            Latitude = dto.Venue.Latitude,
            Longitude = dto.Venue.Longitude
        }
    };
}
