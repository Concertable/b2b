using Dunet;
using Reunion.Errors;

namespace Concertable.B2B.Dashboard.Opportunity.Application;

[Union(EnableImplicitConversions = false)]
internal abstract partial record OpportunityDashboardError : IError
{
    public ErrorDefinition Definition => this switch
    {
        MissingVenue =>
            ErrorDefinition.Forbidden<MissingVenue>("You must have a venue account."),
        MissingArtist =>
            ErrorDefinition.Forbidden<MissingArtist>("You must have an artist account.")
    };

    public partial record MissingVenue;

    public partial record MissingArtist;
}
