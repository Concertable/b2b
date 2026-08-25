using Dunet;
using Reunion.Errors;

namespace Concertable.B2B.Opportunity.Contracts.Errors;

[Union(EnableImplicitConversions = false)]
public abstract partial record FillOpportunityError : IError
{
    public ErrorDefinition Definition => this switch
    {
        Unavailable(var opportunityId) =>
            ErrorDefinition.Conflict<Unavailable>(
                $"Opportunity {opportunityId} is no longer available.")
    };

    public partial record Unavailable(int OpportunityId);
}
