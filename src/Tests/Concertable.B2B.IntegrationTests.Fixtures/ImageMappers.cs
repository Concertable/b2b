using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.IntegrationTests.Fixtures;

public static class ImageMappers
{
    public static async Task AddFileAsync(this MultipartFormDataContent content, IFormFile file, string fieldName)
    {
        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        var fileContent = new ByteArrayContent(stream.ToArray());
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType);
        content.Add(fileContent, fieldName, file.FileName);
    }
}
