using Reunion.Errors;
using Dunet;

namespace Concertable.B2B.Application.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record ApplicationError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NotFound(var applicationId) =>
            ErrorDefinition.NotFound<NotFound>(
                $"Application {applicationId} was not found."),
        OpportunityForbidden(var opportunityId) =>
            ErrorDefinition.Forbidden<OpportunityForbidden>(
                $"You do not own concert opportunity {opportunityId}."),
        MissingArtist =>
            ErrorDefinition.Forbidden<MissingArtist>(
                "You must have an artist account.")
    };

    [ErrorCode("application.get.not_found")]
    public partial record NotFound(int ApplicationId);

    [ErrorCode("application.query.opportunity_forbidden")]
    public partial record OpportunityForbidden(int OpportunityId);

    [ErrorCode("application.query.missing_artist")]
    public partial record MissingArtist;
}
