using Concertable.Kernel.Errors;
using Dunet;

namespace Concertable.B2B.Deal.Contracts.Errors;

[Union(EnableImplicitConversions = false)]
public abstract partial record UpdateDealError : IError
{
    public abstract ErrorDefinition Definition { get; }

    public partial record NotFoundCase(int DealId)
    {
        public override ErrorDefinition Definition => ErrorDefinition.NotFound(
            "deal.update.not_found",
            $"Deal {DealId} was not found.");
    }

    public partial record ValidationCase(ValidationErrors Errors)
    {
        public override ErrorDefinition Definition => ErrorDefinition.Validation(
            "deal.update.invalid",
            "The deal is invalid.",
            Errors.ToDictionary());
    }

    public static UpdateDealError NotFound(int dealId) => new NotFoundCase(dealId);

    public static UpdateDealError Validation(ValidationErrors errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        return new ValidationCase(errors);
    }
}
