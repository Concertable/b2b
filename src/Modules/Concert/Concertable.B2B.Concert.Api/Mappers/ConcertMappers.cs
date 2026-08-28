using Concertable.B2B.Concert.Api.Responses;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Concert.Api.Mappers;

internal static class ConcertMappers
{
    extension(ConcertSummary dto)
    {
        public SummaryResponse ToSummaryResponse() => new()
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
            Venue = new VenueSummaryResponse
            {
                Id = dto.Venue.Id,
                Name = dto.Venue.Name,
                Rating = dto.Venue.Rating
            },
            Artist = new ArtistSummaryResponse
            {
                Id = dto.Artist.Id,
                Name = dto.Artist.Name,
                Rating = dto.Artist.Rating,
                Genres = dto.Artist.Genres.ToList()
            }
        };
    }

    extension(IEnumerable<ConcertSummary> dtos)
    {
        public IEnumerable<SummaryResponse> ToSummaryResponses() => dtos.Select(d => d.ToSummaryResponse());
    }

    extension(ConcertDetails dto)
    {
        public DetailsResponse ToDetailsResponse() => new()
        {
            Id = dto.Id,
            Name = dto.Name,
            About = dto.About,
            BannerUrl = dto.BannerUrl,
            Avatar = dto.Avatar ?? dto.Artist.Avatar,
            Rating = dto.Rating,
            Price = dto.Price,
            TotalTickets = dto.TotalTickets,
            AvailableTickets = dto.AvailableTickets,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            DatePosted = dto.DatePosted,
            Genres = dto.Genres.ToList(),
            Artist = dto.Artist.ToArtistResponse(),
            Venue = dto.Venue.ToVenueResponse()
        };

        public MyDetailsResponse ToMyDetailsResponse() => new()
        {
            Id = dto.Id,
            Name = dto.Name,
            About = dto.About,
            BannerUrl = dto.BannerUrl,
            Avatar = dto.Avatar ?? dto.Artist.Avatar,
            Rating = dto.Rating,
            Price = dto.Price,
            TotalTickets = dto.TotalTickets,
            AvailableTickets = dto.AvailableTickets,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            DatePosted = dto.DatePosted,
            Genres = dto.Genres.ToList(),
            Artist = dto.Artist.ToArtistResponse(),
            Venue = dto.Venue.ToVenueResponse(),
            TicketsSold = dto.TicketsSold,
            DoorRevenue = dto.DoorRevenue,
            Actions = new ConcertActions(
                Cancel: dto.CanCancel
                    ? new ActionLink($"/api/concert/{dto.Id}/cancel", HttpMethods.Post)
                    : null,
                Contract: new ActionLink($"/api/concert/{dto.Id}/contract/pdf", HttpMethods.Get),
                DeclareDoorRevenue: dto.CanDeclareDoorRevenue
                    ? new ActionLink($"/api/concert/{dto.Id}/door-revenue", HttpMethods.Post)
                    : null,
                Invoice: dto.InvoiceId is not null
                    ? new ActionLink($"/api/concert/{dto.Id}/invoice/pdf", HttpMethods.Get)
                    : null)
        };
    }

    extension(ConcertArtist artist)
    {
        private ArtistResponse ToArtistResponse() => new()
        {
            Id = artist.Id,
            Name = artist.Name,
            Avatar = artist.Avatar,
            Rating = artist.Rating,
            County = artist.County,
            Town = artist.Town,
            Genres = artist.Genres.ToList()
        };
    }

    extension(ConcertVenue venue)
    {
        private VenueResponse ToVenueResponse() => new()
        {
            Id = venue.Id,
            Name = venue.Name,
            County = venue.County,
            Town = venue.Town,
            Latitude = venue.Latitude,
            Longitude = venue.Longitude
        };
    }
}
