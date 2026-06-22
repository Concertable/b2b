using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.Venue.Application.Requests;

namespace Concertable.B2B.Venue.IntegrationTests;

internal static class VenueRequestBuilders
{
    internal static CreateVenueRequest BuildCreateRequest(
        string name = "New Venue",
        string about = "About the venue",
        double latitude = 51.5,
        double longitude = -0.1) => new()
    {
        Name = name,
        About = about,
        Latitude = latitude,
        Longitude = longitude,
        Banner = ImageFileBuilder.Jpeg(),
        Avatar = ImageFileBuilder.Jpeg()
    };

    internal static UpdateVenueRequest BuildUpdateRequest(
        string name = "Updated Venue",
        string about = "Updated about",
        double latitude = 51.5,
        double longitude = -0.1) => new()
    {
        Name = name,
        About = about,
        Latitude = latitude,
        Longitude = longitude
    };
}
