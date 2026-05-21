using Concertable.Artist.Application.Requests;

namespace Concertable.Artist.IntegrationTests;

internal static class ArtistMappers
{
    internal static async Task<MultipartFormDataContent> ToFormContent(this CreateArtistRequest req)
    {
        var content = new MultipartFormDataContent
        {
            { new StringContent(req.Name), "Name" },
            { new StringContent(req.About), "About" },
            { new StringContent(req.Latitude.ToString()), "Latitude" },
            { new StringContent(req.Longitude.ToString()), "Longitude" }
        };

        await content.AddFileAsync(req.Banner, "Banner");
        await content.AddFileAsync(req.Avatar, "Avatar");

        return content;
    }

    internal static Task<MultipartFormDataContent> ToFormContent(this UpdateArtistRequest req)
    {
        var content = new MultipartFormDataContent
        {
            { new StringContent(req.Name), "Name" },
            { new StringContent(req.About), "About" },
            { new StringContent(req.Latitude.ToString()), "Latitude" },
            { new StringContent(req.Longitude.ToString()), "Longitude" }
        };

        return Task.FromResult(content);
    }
}
