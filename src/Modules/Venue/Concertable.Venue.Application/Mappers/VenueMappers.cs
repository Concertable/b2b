using Concertable.Shared.Exceptions;
using Concertable.Venue.Application.DTOs;

namespace Concertable.Venue.Application.Mappers;

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
        County = venue.Address?.County ?? string.Empty,
        Town = venue.Address?.Town ?? string.Empty,
        Email = venue.Email ?? string.Empty,
        Latitude = venue.Location?.Y ?? throw new InternalServerException($"Venue {venue.Id} has no location set."),
        Longitude = venue.Location?.X ?? throw new InternalServerException($"Venue {venue.Id} has no location set.")
    };

    public static VenueSummaryDto ToSummaryDto(this VenueEntity venue) => new()
    {
        Id = venue.Id,
        Name = venue.Name,
        Avatar = venue.Avatar,
    };

    public static IEnumerable<VenueDto> ToDtos(this IEnumerable<VenueEntity> venues) =>
        venues.Select(v => v.ToDto());
}
