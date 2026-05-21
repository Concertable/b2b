using Concertable.Artist.Application.Requests;

namespace Concertable.Artist.IntegrationTests;

internal static class ArtistRequestBuilders
{
    internal static CreateArtistRequest BuildCreateRequest(
        string name = "New Artist",
        string about = "About the artist",
        double latitude = 51.5,
        double longitude = -0.1) => new()
    {
        Name = name,
        About = about,
        Latitude = latitude,
        Longitude = longitude,
        Banner = ImageFileBuilder.Jpeg("Banner", "banner.jpg"),
        Avatar = ImageFileBuilder.Jpeg("Avatar", "avatar.jpg")
    };

    internal static UpdateArtistRequest BuildUpdateRequest(
        string name = "Updated Artist",
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
