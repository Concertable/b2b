using Concertable.Kernel.Errors;

namespace Concertable.B2B.Concert.Application.Errors;

internal sealed record OpportunityError : IError
{
    private OpportunityError(ErrorDefinition definition)
    {
        Definition = definition;
    }

    public ErrorDefinition Definition { get; }

    internal static OpportunityError NotFound(int opportunityId) =>
        new(ErrorDefinition.NotFound(
            "opportunity.get.not_found",
            $"Opportunity {opportunityId} was not found."));
}
