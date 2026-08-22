using Reunion.Errors;
using Dunet;

namespace Concertable.B2B.Opportunity.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record OpportunityError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NotFound(var opportunityId) =>
            ErrorDefinition.NotFound<NotFound>(
                $"Opportunity {opportunityId} was not found."),
        MissingVenue =>
            ErrorDefinition.Forbidden<MissingVenue>("You must have a venue account."),
        MissingArtist =>
            ErrorDefinition.Forbidden<MissingArtist>("You must have an artist account.")
    };

    [ErrorCode("opportunity.get.not_found")]
    public partial record NotFound(int OpportunityId);

    [ErrorCode("opportunity.query.missing_venue")]
    public partial record MissingVenue;

    [ErrorCode("opportunity.query.missing_artist")]
    public partial record MissingArtist;
}
