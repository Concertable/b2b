using Reunion.Errors;
using Dunet;

namespace Concertable.B2B.Deal.Contracts.Errors;

[Union(EnableImplicitConversions = false)]
public abstract partial record UpdateDealError : IError
{
    public ErrorDefinition Definition => this switch
    {
        DealNotFound =>
            ErrorDefinition.NotFound<DealNotFound>(),
        Invalid(var errors) =>
            ErrorDefinition.Validation<Invalid>(
                "The deal is invalid.",
                new Reunion.Errors.ValidationErrors(errors.ToDictionary()))
    };

    [ErrorCode("deal.update.not_found")]
    public partial record DealNotFound;

    [ErrorCode("deal.update.invalid")]
    public partial record Invalid(ValidationErrors Errors);
}
