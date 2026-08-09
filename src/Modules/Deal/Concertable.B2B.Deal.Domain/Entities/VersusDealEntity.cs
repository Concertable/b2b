using Reunion.Errors;
using Reunion;

namespace Concertable.B2B.Deal.Domain.Entities;

public sealed class VersusDealEntity : DealEntity
{
    private VersusDealEntity() { }

    public override DealType DealType => DealType.Versus;
    public decimal Guarantee { get; private set; }
    public decimal ArtistDoorPercent { get; private set; }

    public static Result<VersusDealEntity, ValidationErrors> Create(decimal guarantee, decimal artistDoorPercent, PaymentMethod paymentMethod)
    {
        var validation = Validate(guarantee, artistDoorPercent);
        return validation.Bind(() => Result.Success<VersusDealEntity, ValidationErrors>(
            new VersusDealEntity
            {
                Guarantee = guarantee,
                ArtistDoorPercent = artistDoorPercent,
                PaymentMethod = paymentMethod
            }));
    }

    public UnitResult<ValidationErrors> Update(decimal guarantee, decimal artistDoorPercent, PaymentMethod paymentMethod)
    {
        var validation = Validate(guarantee, artistDoorPercent);
        if (validation.IsFailure)
            return validation;

        Guarantee = guarantee;
        ArtistDoorPercent = artistDoorPercent;
        PaymentMethod = paymentMethod;
        return UnitResult.Success<ValidationErrors>();
    }

    private static UnitResult<ValidationErrors> Validate(decimal guarantee, decimal artistDoorPercent)
    {
        var errors = new List<KeyValuePair<string, string>>();

        if (guarantee < 0)
            errors.Add(new(nameof(Guarantee), "Guarantee must be zero or greater."));

        if (artistDoorPercent < 0 || artistDoorPercent > 100)
            errors.Add(new(nameof(ArtistDoorPercent), "Artist door percent must be between 0 and 100."));

        return errors.Count == 0
            ? UnitResult.Success<ValidationErrors>()
            : UnitResult.Failure(new ValidationErrors(errors));
    }

    public decimal CalculateArtistShare(decimal totalRevenue)
        => Guarantee + (totalRevenue * (ArtistDoorPercent / 100));
}
