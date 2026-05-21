namespace Concertable.Contract.Contracts;

public record VersusContract : IContract
{
    public int Id { get; set; }

    public PaymentMethod PaymentMethod { get; set; }
    public ContractType ContractType => ContractType.Versus;
    public decimal Guarantee { get; set; }
    public decimal ArtistDoorPercent { get; set; }
}
