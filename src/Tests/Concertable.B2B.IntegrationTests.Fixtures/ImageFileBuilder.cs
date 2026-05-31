using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Concertable.B2B.IntegrationTests.Fixtures;

public static class ImageFileBuilder
{
    public static FormFile Jpeg(string fieldName = "Image", string fileName = "image.jpg")
    {
        using var image = new Image<Rgba32>(1000, 250);
        var stream = new MemoryStream();
        image.SaveAsJpeg(stream);
        var bytes = stream.ToArray();
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, fieldName, fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg"
        };
    }
}
