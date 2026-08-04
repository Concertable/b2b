using Concertable.Kernel.Errors;

namespace Concertable.B2B.Concert.Application.Errors;

internal sealed record OpportunityError(ErrorDefinition Definition) : IError
{
    internal static OpportunityError NotFound(int opportunityId) =>
        new(ErrorDefinition.NotFound(
            "opportunity.get.not_found",
            $"Opportunity {opportunityId} was not found."));
}
