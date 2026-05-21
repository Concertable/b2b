namespace Concertable.Contract.Contracts;

public record FlatFeeContract : IContract
{
    public int Id { get; set; }

    public PaymentMethod PaymentMethod { get; set; }
    public ContractType ContractType => ContractType.FlatFee;
    public decimal Fee { get; set; }
}
