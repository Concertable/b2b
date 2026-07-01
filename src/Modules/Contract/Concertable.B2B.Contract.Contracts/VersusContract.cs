namespace Concertable.B2B.Contract.Contracts;

public sealed record VersusContract : IContract
{
    public int Id { get; set; }

    public PaymentMethod PaymentMethod { get; set; }
    public ContractType ContractType => ContractType.Versus;
    public decimal Guarantee { get; set; }
    public decimal ArtistDoorPercent { get; set; }
}
