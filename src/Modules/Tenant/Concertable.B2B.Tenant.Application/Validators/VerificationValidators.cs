using Concertable.B2B.Tenant.Application.Requests;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Tenant.Application.Validators;

internal sealed class VerificationDocumentFileValidator : AbstractValidator<IFormFile>
{
    // Magic bytes for each allowed type — the declared ContentType header is attacker-controlled, so the
    // allowlist alone (below) is not enough; confirm the bytes actually match the claimed type.
    private static readonly Dictionary<string, byte[]> MagicBytesByContentType = new()
    {
        ["application/pdf"] = "%PDF-"u8.ToArray(),
        ["image/jpeg"] = [0xFF, 0xD8, 0xFF],
        ["image/png"] = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
    };

    private const long MaxFileSize = 10 * 1024 * 1024;

    public VerificationDocumentFileValidator()
    {
        RuleFor(x => x.ContentType)
            .Must(ct => MagicBytesByContentType.ContainsKey(ct))
            .WithMessage("Evidence must be a PDF, JPEG or PNG file.");

        RuleFor(x => x.Length)
            .GreaterThan(0)
            .LessThanOrEqualTo(MaxFileSize)
            .WithMessage("Evidence file exceeds the maximum size of 10MB.");

        RuleFor(x => x)
            .Must(HasMatchingMagicBytes)
            .When(x => MagicBytesByContentType.ContainsKey(x.ContentType))
            .WithMessage("The file's content does not match its declared type.");
    }

    private static bool HasMatchingMagicBytes(IFormFile file)
    {
        var magicBytes = MagicBytesByContentType[file.ContentType];
        using var stream = file.OpenReadStream();
        var buffer = new byte[magicBytes.Length];
        var read = stream.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false);
        return read == magicBytes.Length && buffer.AsSpan().SequenceEqual(magicBytes);
    }
}

internal sealed class SubmitVerificationRequestValidator : AbstractValidator<SubmitVerificationRequest>
{
    public SubmitVerificationRequestValidator()
    {
        RuleFor(x => x.Files)
            .NotEmpty()
            .WithMessage("At least one evidence document is required.");

        RuleForEach(x => x.Files).SetValidator(new VerificationDocumentFileValidator());

        RuleForEach(x => x.DocumentTypes).IsInEnum();

        RuleFor(x => x)
            .Must(x => x.Files.Count == x.DocumentTypes.Count)
            .WithMessage("Each evidence document requires exactly one document type.");
    }
}
