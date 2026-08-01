using Concertable.Kernel.Errors;

namespace Concertable.B2B.Concert.Application.Errors;

internal sealed record OpportunityMutationError : IError
{
    private OpportunityMutationError(ErrorDefinition definition)
    {
        this.Definition = definition;
    }

    public ErrorDefinition Definition { get; }

    internal static OpportunityMutationError InvalidDeal(ValidationErrors errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        return new OpportunityMutationError(ErrorDefinition.Validation(
            "opportunity.deal.invalid",
            "The opportunity deal is invalid.",
            errors.ToDictionary()));
    }
}
