namespace Concertable.B2B.Concert.Application.Errors;

internal sealed record ApplyApplicationError : IError
{
    private ApplyApplicationError(ErrorDefinition definition)
    {
        Definition = definition;
    }

    public ErrorDefinition Definition { get; }

    internal static ApplyApplicationError MissingArtist() =>
        new(ErrorDefinition.Forbidden(
            "application.apply.missing_artist",
            "You must create an artist account before applying for a concert opportunity."));

    internal static ApplyApplicationError OpportunityNotFound(int opportunityId) =>
        new(ErrorDefinition.NotFound(
            "application.apply.opportunity_not_found",
            $"Concert opportunity {opportunityId} was not found."));

    internal static ApplyApplicationError AlreadyApplied() =>
        new(ErrorDefinition.Invalid(
            "application.apply.duplicate",
            "You have already applied to this concert opportunity."));

    internal static ApplyApplicationError Invalid(ValidationErrors errors) =>
        new(ErrorDefinition.Validation(
            "application.apply.invalid",
            "The application is not eligible.",
            errors.ToDictionary()));

    internal static ApplyApplicationError GenreMismatch() =>
        new(ErrorDefinition.Invalid(
            "application.apply.genre_mismatch",
            "Your artist must share a genre with this concert opportunity."));

    internal static ApplyApplicationError UnsupportedDeal(DealType dealType) =>
        new(ErrorDefinition.Invalid(
            "application.apply.unsupported_deal",
            $"Deal {dealType} does not support applications."));

    internal static ApplyApplicationError MissingTenant() =>
        new(ErrorDefinition.Forbidden(
            "application.apply.missing_tenant",
            "No active organization was found for the current user."));

    internal static ApplyApplicationError MissingUser() =>
        new(ErrorDefinition.Forbidden(
            "application.apply.missing_user",
            "No user was found for the current request."));
}
