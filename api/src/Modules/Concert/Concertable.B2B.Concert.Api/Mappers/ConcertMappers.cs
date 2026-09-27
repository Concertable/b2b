using Concertable.B2B.Concert.Api.Responses;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Concert.Api.Mappers;

internal static class ConcertMappers
{
    extension(PublishedConcert concert)
    {
        public PublishedConcertResponse ToResponse() =>
            new(
                concert.Id,
                concert.Name,
                concert.About,
                concert.StartsAt,
                concert.EndsAt,
                concert.VenueName,
                concert.ArtistName,
                concert.Price);
    }

    extension(IEnumerable<PublishedConcert> concerts)
    {
        public IEnumerable<PublishedConcertResponse> ToResponses() =>
            concerts.Select(concert => concert.ToResponse());
    }

    extension(ConcertSummary concert)
    {
        public SummaryResponse ToResponse() =>
            new(
                concert.Id,
                concert.Name,
                concert.StartDate,
                concert.EndDate,
                concert.VenueName,
                concert.ArtistName,
                concert.State);
    }

    extension(IEnumerable<ConcertSummary> concerts)
    {
        public IEnumerable<SummaryResponse> ToSummaryResponses() =>
            concerts.Select(concert => concert.ToResponse());
    }

    extension(ConcertOperations concert)
    {
        public OperationsResponse ToResponse() => new()
        {
            Id = concert.Id,
            ApplicationId = concert.ApplicationId,
            Name = concert.Name,
            About = concert.About,
            BannerUrl = concert.BannerUrl,
            Avatar = concert.Avatar ?? concert.Artist.Avatar,
            Rating = concert.Rating,
            TotalTickets = concert.TotalTickets,
            AvailableTickets = concert.AvailableTickets,
            Price = concert.Price,
            StartDate = concert.StartDate,
            EndDate = concert.EndDate,
            DatePosted = concert.DatePosted,
            State = concert.State,
            Genres = concert.Genres.ToList(),
            Artist = concert.Artist.ToResponse(),
            Venue = concert.Venue.ToResponse(),
            Actions = new ConcertOperationsActions(
                concert.CanCancel
                    ? new ActionLink($"/api/concert/{concert.Id}/cancel", HttpMethods.Post)
                    : null)
        };
    }

    extension(ConcertFinance concert)
    {
        public FinanceResponse ToResponse() =>
            new(
                concert.Id,
                concert.TicketsSold,
                concert.DoorRevenue,
                concert.IsRevenueShare,
                new ConcertFinanceActions(
                    concert.CanDeclareDoorRevenue
                        ? new ActionLink($"/api/concert/{concert.Id}/door-revenue", HttpMethods.Post)
                        : null,
                    concert.InvoiceId is not null
                        ? new ActionLink($"/api/concert/{concert.Id}/invoice/pdf", HttpMethods.Get)
                        : null));
    }

    extension(ConcertArtist artist)
    {
        private ArtistResponse ToResponse() => new()
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
        private VenueResponse ToResponse() => new()
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
