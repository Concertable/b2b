using Concertable.B2B.Tenant.Application.Requests;
using Concertable.B2B.Tenant.Application.Validators;
using Concertable.B2B.Tenant.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Tenant.UnitTests;

public sealed class VerificationValidatorsTests
{
    private readonly VerificationDocumentFileValidator fileValidator = new();
    private readonly SubmitVerificationRequestValidator requestValidator = new();

    private static FormFile Build(byte[] bytes, string contentType, string fileName = "evidence") =>
        new(new MemoryStream(bytes), 0, bytes.Length, "Files", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType,
        };

    private static byte[] RealPdfBytes() => "%PDF-1.4\n%%EOF"u8.ToArray();

    private static byte[] RealJpegBytes() => [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];

    #region VerificationDocumentFileValidator

    [Fact]
    public void Validate_DisallowedContentType_IsInvalid()
    {
        var file = Build(RealPdfBytes(), "text/plain");

        var result = fileValidator.Validate(file);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyFile_IsInvalid()
    {
        var file = Build([], "application/pdf");

        var result = fileValidator.Validate(file);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_RealPdfBytes_IsValid()
    {
        var file = Build(RealPdfBytes(), "application/pdf");

        var result = fileValidator.Validate(file);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_PdfContentTypeWithNonPdfBytes_IsInvalid()
    {
        var file = Build("not a pdf"u8.ToArray(), "application/pdf");

        var result = fileValidator.Validate(file);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_RealJpegBytes_IsValid()
    {
        var file = Build(RealJpegBytes(), "image/jpeg");

        var result = fileValidator.Validate(file);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_JpegContentTypeWithNonImageBytes_IsInvalid()
    {
        var file = Build("not an image"u8.ToArray(), "image/jpeg");

        var result = fileValidator.Validate(file);

        Assert.False(result.IsValid);
    }

    #endregion

    #region SubmitVerificationRequestValidator

    [Fact]
    public void Validate_MismatchedFileAndDocumentTypeCounts_IsInvalid()
    {
        var request = new SubmitVerificationRequest
        {
            Files = new FormFileCollection { Build(RealPdfBytes(), "application/pdf") },
            DocumentTypes = [VerificationDocumentType.Licence, VerificationDocumentType.ProofOfAddress],
        };

        var result = requestValidator.Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_NoFiles_IsInvalid()
    {
        var request = new SubmitVerificationRequest { Files = new FormFileCollection(), DocumentTypes = [] };

        var result = requestValidator.Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_MatchingFilesAndDocumentTypes_IsValid()
    {
        var request = new SubmitVerificationRequest
        {
            Files = new FormFileCollection { Build(RealPdfBytes(), "application/pdf") },
            DocumentTypes = [VerificationDocumentType.Licence],
        };

        var result = requestValidator.Validate(request);

        Assert.True(result.IsValid);
    }

    #endregion
}
