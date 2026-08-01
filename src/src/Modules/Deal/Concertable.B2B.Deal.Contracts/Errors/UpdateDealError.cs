using Concertable.Kernel.Errors;
using Dunet;

namespace Concertable.B2B.Deal.Contracts.Errors;

[Union]
public partial record UpdateDealError : IError
{
    partial record NotFoundCase(int DealId);
    partial record ValidationCase(ValidationErrors Errors);

    public static UpdateDealError NotFound(int dealId) => new NotFoundCase(dealId);

    public static UpdateDealError Validation(ValidationErrors errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        return new ValidationCase(errors);
    }

    public ErrorDefinition Definition => Match<ErrorDefinition>(
        notFound => ErrorDefinition.NotFound(
            "deal.update.not_found",
            $"Deal {notFound.DealId} was not found."),
        validation => ErrorDefinition.Validation(
            "deal.update.invalid",
            "The deal is invalid.",
            validation.Errors.ToDictionary()));
}
