namespace Concertable.B2B.Deal.Domain.Entities;

public sealed class VenueHireDealEntity : DealEntity
{
    private VenueHireDealEntity() { }

    public override DealType ContractType => DealType.VenueHire;
    public decimal HireFee { get; private set; }

    public static VenueHireDealEntity Create(decimal hireFee, PaymentMethod paymentMethod)
    {
        ValidateFee(hireFee);
        return new() { HireFee = hireFee, PaymentMethod = paymentMethod };
    }

    public void Update(decimal hireFee, PaymentMethod paymentMethod)
    {
        ValidateFee(hireFee);

        HireFee = hireFee;
        PaymentMethod = paymentMethod;
    }

    private static void ValidateFee(decimal hireFee)
    {
        if (hireFee <= 0)
            throw new DomainException("Hire fee must be greater than zero.");
    }
}
