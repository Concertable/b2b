using Concertable.B2B.Venue.Application.DTOs;

namespace Concertable.B2B.Venue.Application.Mappers;

public static class VenueMappers
{
    public static VenueDto ToDto(this VenueEntity venue) => new()
    {
        Id = venue.Id,
        Name = venue.Name,
        About = venue.About,
        BannerUrl = venue.BannerUrl,
        Avatar = venue.Avatar,
        Approved = venue.Approved,
        County = venue.Address.County,
        Town = venue.Address.Town,
        Email = venue.Email,
        Latitude = venue.Location.Y,
        Longitude = venue.Location.X
    };
}
