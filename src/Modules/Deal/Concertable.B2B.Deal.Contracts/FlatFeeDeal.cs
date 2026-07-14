namespace Concertable.B2B.Deal.Contracts;

public sealed record FlatFeeDeal : IDeal
{
    public int Id { get; set; }

    public PaymentMethod PaymentMethod { get; set; }
    public DealType ContractType => DealType.FlatFee;
    public decimal Fee { get; set; }
}
