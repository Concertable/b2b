using Reunion.Errors;
using Reunion;

namespace Concertable.B2B.Deal.Domain.Entities;

public sealed class VenueHireDealEntity : DealEntity
{
    private VenueHireDealEntity() { }

    public override DealType DealType => DealType.VenueHire;
    public decimal HireFee { get; private set; }

    public static Result<VenueHireDealEntity, ValidationErrors> Create(decimal hireFee, PaymentMethod paymentMethod)
    {
        var validation = ValidateFee(hireFee);
        return validation.Bind(() => Result.Success<VenueHireDealEntity, ValidationErrors>(
            new VenueHireDealEntity { HireFee = hireFee, PaymentMethod = paymentMethod }));
    }

    public UnitResult<ValidationErrors> Update(decimal hireFee, PaymentMethod paymentMethod)
    {
        var validation = ValidateFee(hireFee);
        if (validation.IsFailure)
            return validation;

        HireFee = hireFee;
        PaymentMethod = paymentMethod;
        return UnitResult.Success<ValidationErrors>();
    }

    private static UnitResult<ValidationErrors> ValidateFee(decimal hireFee) =>
        hireFee > 0
            ? UnitResult.Success<ValidationErrors>()
            : UnitResult.Failure(new ValidationErrors(
                [new(nameof(HireFee), "Hire fee must be greater than zero.")]));
}
