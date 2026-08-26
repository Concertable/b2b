using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.Tenant.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Tenant.IntegrationTests;

internal static class VerificationMappers
{
    internal static async Task<MultipartFormDataContent> ToFormContent(
        this IReadOnlyList<(IFormFile File, VerificationDocumentType DocumentType)> documents)
    {
        var content = new MultipartFormDataContent();
        foreach (var (file, documentType) in documents)
        {
            await content.AddFileAsync(file, "Files");
            content.Add(new StringContent(documentType.ToString()), "DocumentTypes");
        }

        return content;
    }
}
