using Concertable.B2B.Tenant.Application.Requests;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Tenant.Application.Validators;

internal sealed class VerificationDocumentFileValidator : AbstractValidator<IFormFile>
{
    private static readonly string[] AllowedMimeTypes = ["application/pdf", "image/jpeg", "image/png"];
    private const long MaxFileSize = 10 * 1024 * 1024;

    public VerificationDocumentFileValidator()
    {
        RuleFor(x => x.ContentType)
            .Must(ct => AllowedMimeTypes.Contains(ct))
            .WithMessage("Evidence must be a PDF, JPEG or PNG file.");

        RuleFor(x => x.Length)
            .GreaterThan(0)
            .LessThanOrEqualTo(MaxFileSize)
            .WithMessage("Evidence file exceeds the maximum size of 10MB.");
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
