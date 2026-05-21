namespace Concertable.Contract.Domain;

public class VenueHireContractEntity : ContractEntity
{
    private VenueHireContractEntity() { }

    public override ContractType ContractType => ContractType.VenueHire;
    public decimal HireFee { get; private set; }

    public static VenueHireContractEntity Create(decimal hireFee, PaymentMethod paymentMethod)
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
