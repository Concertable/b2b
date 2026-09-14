using System.Net.Http.Headers;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.Venue.Application.Requests;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Venue.IntegrationTests;

internal static class VenueMappers
{
    internal static async Task<MultipartFormDataContent> ToFormContent(this CreateVenueRequest req)
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

    internal static async Task<MultipartFormDataContent> ToFormContent(this UpdateVenueRequest req)
    {
        var content = new MultipartFormDataContent
        {
            { new StringContent(req.Name), "Name" },
            { new StringContent(req.About), "About" },
            { new StringContent(req.Latitude.ToString()), "Latitude" },
            { new StringContent(req.Longitude.ToString()), "Longitude" }
        };

        if (req.Banner is not null)
            await content.AddFileAsync(req.Banner, "Banner");

        return content;
    }
}
