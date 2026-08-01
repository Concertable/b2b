using Concertable.Kernel.Errors;
using Dunet;

namespace Concertable.B2B.Deal.Contracts.Errors;

[Union]
public partial record CreateDealError : IError
{
    partial record ValidationCase(ValidationErrors Errors);

    public static CreateDealError Validation(ValidationErrors errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        return new ValidationCase(errors);
    }

    public ErrorDefinition Definition => Match<ErrorDefinition>(
        validation => ErrorDefinition.Validation(
            "deal.create.invalid",
            "The deal is invalid.",
            validation.Errors.ToDictionary()));
}
