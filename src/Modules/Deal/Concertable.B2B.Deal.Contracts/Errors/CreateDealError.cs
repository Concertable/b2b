using Reunion.Errors;
using Dunet;

namespace Concertable.B2B.Deal.Contracts.Errors;

[Union(EnableImplicitConversions = false)]
public abstract partial record CreateDealError : IError
{
    public ErrorDefinition Definition => this switch
    {
        Invalid(var errors) =>
            ErrorDefinition.For<CreateDealError>().Validation<Invalid>(
                "The deal is invalid.",
                new Reunion.Errors.ValidationErrors(errors.ToDictionary()))
    };

    [ErrorCode("deal.create.invalid")]
    public partial record Invalid(ValidationErrors Errors);
}
