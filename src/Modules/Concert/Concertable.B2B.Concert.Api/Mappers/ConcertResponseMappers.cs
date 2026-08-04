using Concertable.B2B.Concert.Api.Responses;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Concert.Api.Mappers;

internal static class ConcertResponseMappers
{
    public static SummaryResponse ToSummaryResponse(this ConcertSummary dto) => new()
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

    public static IEnumerable<SummaryResponse> ToSummaryResponses(this IEnumerable<ConcertSummary> dtos) =>
        dtos.Select(d => d.ToSummaryResponse());

    public static DetailsResponse ToDetailsResponse(this ConcertDetails dto) => new()
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

    /// <summary>
    /// The owner (party-scoped) read: the public details plus the party-only action links and
    /// venue-private figures the anonymous read omits. Cancel is offered only while Booked; the contract
    /// is frozen at accept so it always exists; DeclareDoorRevenue shows only for an ended, still-Booked,
    /// undeclared revenue-share gig.
    /// </summary>
    public static MyDetailsResponse ToMyDetailsResponse(this ConcertDetails dto, DateTime utcNow) => new()
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
            Cancel: dto.State == LifecycleState.Booked
                ? new ActionLink($"/api/Concert/{dto.Id}/cancel", HttpMethods.Post)
                : null,
            Contract: new ActionLink($"/api/Concert/{dto.Id}/contract/pdf", HttpMethods.Get),
            DeclareDoorRevenue: dto.State == LifecycleState.Booked
                && dto.IsRevenueShare && dto.DoorRevenue is null && dto.EndDate < utcNow
                ? new ActionLink($"/api/Concert/{dto.Id}/door-revenue", HttpMethods.Post)
                : null,
            Invoice: dto.InvoiceId is not null
                ? new ActionLink($"/api/Concert/{dto.Id}/invoice/pdf", HttpMethods.Get)
                : null)
    };

    private static ArtistResponse ToArtistResponse(this ConcertArtist artist) => new()
    {
        Id = artist.Id,
        Name = artist.Name,
        Avatar = artist.Avatar,
        Rating = artist.Rating,
        County = artist.County,
        Town = artist.Town,
        Genres = artist.Genres.ToList()
    };

    private static VenueResponse ToVenueResponse(this ConcertVenue venue) => new()
    {
        Id = venue.Id,
        Name = venue.Name,
        County = venue.County,
        Town = venue.Town,
        Latitude = venue.Latitude,
        Longitude = venue.Longitude
    };
}
