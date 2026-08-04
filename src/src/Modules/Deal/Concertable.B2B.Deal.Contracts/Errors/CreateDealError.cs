using Concertable.Kernel.Errors;
using Dunet;

namespace Concertable.B2B.Deal.Contracts.Errors;

[Union(EnableImplicitConversions = false)]
public abstract partial record CreateDealError : IError
{
    public abstract ErrorDefinition Definition { get; }

    [ErrorCode("deal.create.invalid")]
    public partial record Invalid(ValidationErrors Errors)
    {
        public override ErrorDefinition Definition => ErrorDefinition.Validation<Invalid>(
            "The deal is invalid.",
            Errors.ToDictionary());
    }
}
