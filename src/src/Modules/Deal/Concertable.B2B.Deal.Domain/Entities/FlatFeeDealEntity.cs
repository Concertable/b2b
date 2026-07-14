namespace Concertable.B2B.Deal.Domain.Entities;

public sealed class FlatFeeDealEntity : DealEntity
{
    private FlatFeeDealEntity() { }

    public override DealType ContractType => DealType.FlatFee;
    public decimal Fee { get; private set; }

    public static FlatFeeDealEntity Create(decimal fee, PaymentMethod paymentMethod)
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
