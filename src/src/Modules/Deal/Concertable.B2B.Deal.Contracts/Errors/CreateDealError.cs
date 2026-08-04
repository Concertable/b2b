using Concertable.Kernel.Errors;
using Dunet;

namespace Concertable.B2B.Deal.Contracts.Errors;

[Union(EnableImplicitConversions = false)]
public abstract partial record CreateDealError : IError
{
    public abstract ErrorDefinition Definition { get; }

    public partial record ValidationCase(ValidationErrors Errors)
    {
        public override ErrorDefinition Definition => ErrorDefinition.Validation(
            "deal.create.invalid",
            "The deal is invalid.",
            Errors.ToDictionary());
    }

    public static CreateDealError Validation(ValidationErrors errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        return new ValidationCase(errors);
    }
}
