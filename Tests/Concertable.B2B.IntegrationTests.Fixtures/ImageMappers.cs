using System.Net.Http.Headers;
using Concertable.Contracts;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.IntegrationTests.Fixtures;

public static class ImageMappers
{
    public static async Task<MultipartFormDataContent> ToFormContent(this ImageDto image)
    {
        using var imageStream = new MemoryStream();
        await image.File.CopyToAsync(imageStream);
        var fileContent = new ByteArrayContent(imageStream.ToArray());
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(image.File.ContentType);

        return new MultipartFormDataContent
        {
            { new StringContent(image.Url), "Image.Url" },
            { fileContent, "Image.File", image.File.FileName }
        };
    }

    public static async Task AddFileAsync(this MultipartFormDataContent content, IFormFile file, string fieldName)
    {
        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        var fileContent = new ByteArrayContent(stream.ToArray());
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType);
        content.Add(fileContent, fieldName, file.FileName);
    }
}
