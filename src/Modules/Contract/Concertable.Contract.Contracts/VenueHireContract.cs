namespace Concertable.Contract.Contracts;

public record VenueHireContract : IContract
{
    public int Id { get; set; }

    public PaymentMethod PaymentMethod { get; set; }
    public ContractType ContractType => ContractType.VenueHire;
    public decimal HireFee { get; set; }
}
