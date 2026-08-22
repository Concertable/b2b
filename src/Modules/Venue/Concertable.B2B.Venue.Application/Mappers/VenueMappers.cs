using Concertable.B2B.Venue.Application.DTOs;

namespace Concertable.B2B.Venue.Application.Mappers;

internal static class VenueMappers
{
    public static PendingVenueDto ToPendingVenueDto(this VenueEntity venue) => new()
    {
        Id = venue.Id,
        Name = venue.Name,
        Email = venue.Email,
        Avatar = venue.Avatar,
        County = venue.Address.County,
        Town = venue.Address.Town
    };
}
