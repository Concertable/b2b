namespace Concertable.B2B.Concert.Application.Errors;

internal sealed record ApplicationEligibilityError : IError
{
    private ApplicationEligibilityError(ErrorDefinition definition)
    {
        Definition = definition;
    }

    public ErrorDefinition Definition { get; }

    internal static ApplicationEligibilityError MissingArtist() =>
        new(ErrorDefinition.Forbidden(
            "application.eligibility.missing_artist",
            "You must have an artist account to apply for a concert opportunity"));

    internal static ApplicationEligibilityError OpportunityNotFound() =>
        new(ErrorDefinition.NotFound(
            "application.eligibility.opportunity_not_found",
            "Concert opportunity does not exist"));

    internal static ApplicationEligibilityError ApplicationNotFound() =>
        new(ErrorDefinition.NotFound(
            "application.eligibility.application_not_found",
            "Concert application does not exist"));

    internal static ApplicationEligibilityError Invalid(ValidationErrors errors) =>
        new(ErrorDefinition.Validation(
            "application.eligibility.invalid",
            "The application is not eligible.",
            errors.ToDictionary()));
}
