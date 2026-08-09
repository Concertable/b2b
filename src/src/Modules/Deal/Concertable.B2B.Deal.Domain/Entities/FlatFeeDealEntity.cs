using Reunion.Errors;
using Reunion;

namespace Concertable.B2B.Deal.Domain.Entities;

public sealed class FlatFeeDealEntity : DealEntity
{
    private FlatFeeDealEntity() { }

    public override DealType DealType => DealType.FlatFee;
    public decimal Fee { get; private set; }

    public static Result<FlatFeeDealEntity, ValidationErrors> Create(decimal fee, PaymentMethod paymentMethod)
    {
        var validation = ValidateFee(fee);
        return validation.Bind(() => Result.Success<FlatFeeDealEntity, ValidationErrors>(
            new FlatFeeDealEntity { Fee = fee, PaymentMethod = paymentMethod }));
    }

    public UnitResult<ValidationErrors> Update(decimal fee, PaymentMethod paymentMethod)
    {
        var validation = ValidateFee(fee);
        if (validation.IsFailure)
            return validation;

        Fee = fee;
        PaymentMethod = paymentMethod;
        return UnitResult.Success<ValidationErrors>();
    }

    private static UnitResult<ValidationErrors> ValidateFee(decimal fee) =>
        fee > 0
            ? UnitResult.Success<ValidationErrors>()
            : UnitResult.Failure(new ValidationErrors(
                [new(nameof(Fee), "Fee must be greater than zero.")]));
}
