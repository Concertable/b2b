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
                $"Opportunity {opportunityId} was not found.")
    };

    public partial record NotFound(int OpportunityId);
}
