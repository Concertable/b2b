using Reunion.Errors;
using Dunet;

namespace Concertable.B2B.Deal.Contracts.Errors;

[Union(EnableImplicitConversions = false)]
public abstract partial record CreateDealError : IError
{
    public ErrorDefinition Definition => this switch
    {
        Invalid(var errors) =>
            ErrorDefinition.Validation<Invalid>(
                "The deal is invalid.",
                errors)
    };

    [ErrorCode("deal.create.invalid")]
    public partial record Invalid(ValidationErrors Errors);
}
