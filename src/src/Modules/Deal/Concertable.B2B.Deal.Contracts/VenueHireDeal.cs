namespace Concertable.B2B.Deal.Contracts;

public sealed record VenueHireDeal : IDeal
{
    public int Id { get; set; }

    public PaymentMethod PaymentMethod { get; set; }
    public DealType ContractType => DealType.VenueHire;
    public decimal HireFee { get; set; }
}
