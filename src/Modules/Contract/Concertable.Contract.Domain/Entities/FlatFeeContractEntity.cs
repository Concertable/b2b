namespace Concertable.Contract.Domain;

public class FlatFeeContractEntity : ContractEntity
{
    private FlatFeeContractEntity() { }

    public override ContractType ContractType => ContractType.FlatFee;
    public decimal Fee { get; private set; }

    public static FlatFeeContractEntity Create(decimal fee, PaymentMethod paymentMethod)
    {
        ValidateFee(fee);
        return new() { Fee = fee, PaymentMethod = paymentMethod };
    }

    public void Update(decimal fee, PaymentMethod paymentMethod)
    {
        ValidateFee(fee);

        Fee = fee;
        PaymentMethod = paymentMethod;
    }

    private static void ValidateFee(decimal fee)
    {
        if (fee <= 0)
            throw new DomainException("Fee must be greater than zero.");
    }
}
