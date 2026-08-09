using Dunet;

namespace Concertable.B2B.Concert.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record ApplicationEligibilityError : IError
{
    public ErrorDefinition Definition => this switch
    {
        MissingArtist =>
            ErrorDefinition.For<ApplicationEligibilityError>().Forbidden<MissingArtist>(
                "You must have an artist account to apply for a concert opportunity"),
        OpportunityNotFound =>
            ErrorDefinition.For<ApplicationEligibilityError>().NotFound<OpportunityNotFound>(
                "Concert opportunity does not exist"),
        ApplicationNotFound =>
            ErrorDefinition.For<ApplicationEligibilityError>().NotFound<ApplicationNotFound>(
                "Concert application does not exist"),
        Invalid(var errors) =>
            ErrorDefinition.For<ApplicationEligibilityError>().Validation<Invalid>(
                "The application is not eligible.",
                new Reunion.Errors.ValidationErrors(errors.ToDictionary()))
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
