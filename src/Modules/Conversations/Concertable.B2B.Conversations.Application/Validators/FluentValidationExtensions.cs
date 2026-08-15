using Reunion.Errors;
using Reunion.Validation;
using FluentValidationResult = FluentValidation.Results.ValidationResult;

namespace Concertable.B2B.Conversations.Application.Validators;

internal static class FluentValidationExtensions
{
    extension(FluentValidationResult result)
    {
        /// <summary>
        /// Adapts a FluentValidation run to the Reunion carrier an operation's error union holds. Field
        /// keys are camelCased so the failure keys match the JSON request they came from.
        /// </summary>
        public ValidationResult ToValidationResult() =>
            result.IsValid
                ? ValidationResult.Valid()
                : ValidationResult.Invalid(new ValidationErrors(
                    result.Errors.Select(e =>
                        new KeyValuePair<string, string>(CamelCase(e.PropertyName), e.ErrorMessage))));
    }

    private static string CamelCase(string propertyName) =>
        string.IsNullOrEmpty(propertyName)
            ? propertyName
            : char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
}
