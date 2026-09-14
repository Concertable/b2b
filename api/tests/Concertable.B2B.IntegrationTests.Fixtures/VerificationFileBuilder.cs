using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.IntegrationTests.Fixtures;

public static class VerificationFileBuilder
{
    public static FormFile Pdf(string fileName = "licence.pdf", int sizeBytes = 1024)
    {
        var body = new byte[Math.Max(sizeBytes - "%PDF-".Length, 0)];
        var bytes = "%PDF-"u8.ToArray().Concat(body).ToArray();
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "Files", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };
    }

    public static FormFile TextFile(string fileName = "notes.txt")
    {
        var bytes = "not evidence"u8.ToArray();
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "Files", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };
    }
}
