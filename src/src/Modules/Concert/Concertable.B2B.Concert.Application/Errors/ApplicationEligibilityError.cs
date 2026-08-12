using Dunet;

namespace Concertable.B2B.Concert.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record ApplicationEligibilityError : IError
{
    public ErrorDefinition Definition => this switch
    {
        MissingArtist =>
            ErrorDefinition.Forbidden<MissingArtist>(
                "You must have an artist account to apply for a concert opportunity"),
        OpportunityNotFound =>
            ErrorDefinition.NotFound<OpportunityNotFound>(
                "Concert opportunity does not exist"),
        ApplicationNotFound =>
            ErrorDefinition.NotFound<ApplicationNotFound>(
                "Concert application does not exist"),
        Invalid(var errors) =>
            ErrorDefinition.Validation<Invalid>(
                "The application is not eligible.",
                errors)
    };

    [ErrorCode("application.eligibility.missing_artist")]
    public partial record MissingArtist;

    [ErrorCode("application.eligibility.opportunity_not_found")]
    public partial record OpportunityNotFound;

    [ErrorCode("application.eligibility.application_not_found")]
    public partial record ApplicationNotFound;

    [ErrorCode("application.eligibility.invalid")]
    public partial record Invalid(ValidationErrors Errors);
}
