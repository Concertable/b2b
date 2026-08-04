using System.ComponentModel;
using Concertable.Kernel.Errors;
using Dunet;

namespace Concertable.B2B.Deal.Contracts.Errors;

[Union(EnableImplicitConversions = false)]
public abstract partial record UpdateDealError : IError
{
    public abstract ErrorDefinition Definition { get; }

    [DisplayName("Deal")]
    [ErrorCode("deal.update.not_found")]
    public partial record DealNotFound
    {
        public override ErrorDefinition Definition => ErrorDefinition.NotFound<DealNotFound>();
    }

    [ErrorCode("deal.update.invalid")]
    public partial record Invalid(ValidationErrors Errors)
    {
        public override ErrorDefinition Definition => ErrorDefinition.Validation<Invalid>(
            "The deal is invalid.",
            Errors.ToDictionary());
    }
}
