using Dunet;

namespace Concertable.B2B.Concert.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record ApplyApplicationError : IError
{
    public ErrorDefinition Definition => this switch
    {
        MissingArtist =>
            ErrorDefinition.For<ApplyApplicationError>().Forbidden<MissingArtist>(
                "You must create an artist account before applying for a concert opportunity."),
        OpportunityNotFound(var opportunityId) =>
            ErrorDefinition.For<ApplyApplicationError>().NotFound<OpportunityNotFound>(
                $"Concert opportunity {opportunityId} was not found."),
        AlreadyApplied =>
            ErrorDefinition.For<ApplyApplicationError>().Invalid<AlreadyApplied>(
                "You have already applied to this concert opportunity."),
        Invalid(var errors) =>
            ErrorDefinition.For<ApplyApplicationError>().Validation<Invalid>(
                "The application is not eligible.",
                new Reunion.Errors.ValidationErrors(errors.ToDictionary())),
        GenreMismatch =>
            ErrorDefinition.For<ApplyApplicationError>().Invalid<GenreMismatch>(
                "Your artist must share a genre with this concert opportunity."),
        UnsupportedDeal(var dealType) =>
            ErrorDefinition.For<ApplyApplicationError>().Invalid<UnsupportedDeal>(
                $"Deal {dealType} does not support applications."),
        MissingTenant =>
            ErrorDefinition.For<ApplyApplicationError>().Forbidden<MissingTenant>(
                "No active organization was found for the current user."),
        MissingUser =>
            ErrorDefinition.For<ApplyApplicationError>().Forbidden<MissingUser>(
                "No user was found for the current request.")
    };

    [ErrorCode("application.apply.missing_artist")]
    public partial record MissingArtist;

    [ErrorCode("application.apply.opportunity_not_found")]
    public partial record OpportunityNotFound(int OpportunityId);

    [ErrorCode("application.apply.duplicate")]
    public partial record AlreadyApplied;

    [ErrorCode("application.apply.invalid")]
    public partial record Invalid(ValidationErrors Errors);

    [ErrorCode("application.apply.genre_mismatch")]
    public partial record GenreMismatch;

    [ErrorCode("application.apply.unsupported_deal")]
    public partial record UnsupportedDeal(DealType DealType);

    [ErrorCode("application.apply.missing_tenant")]
    public partial record MissingTenant;

    [ErrorCode("application.apply.missing_user")]
    public partial record MissingUser;
}
