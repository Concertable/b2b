using Concertable.B2B.Conversations.Application.Requests;
using Reunion.Errors;
using Reunion.Validation;

namespace Concertable.B2B.Conversations.Application.Validators;

internal static class ContentReportValidators
{
    public const int MaxDetailsLength = 2000;

    public static ValidationResult Validate(ReportMessageRequest request) =>
        new[]
        {
            Validate(
                Enum.IsDefined(request.Category),
                "category",
                "Select a valid report category."),
            Validate(
                request.Details is null || request.Details.Length <= MaxDetailsLength,
                "details",
                $"Details must be {MaxDetailsLength} characters or fewer.")
        }.Combine();

    private static ValidationResult Validate(bool isValid, string field, string message) =>
        isValid
            ? ValidationResult.Valid()
            : ValidationResult.Invalid(new ValidationErrors([new(field, message)]));
}
