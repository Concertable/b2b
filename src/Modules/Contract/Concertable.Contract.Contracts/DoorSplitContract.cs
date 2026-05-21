namespace Concertable.Contract.Contracts;

public record DoorSplitContract : IContract
{
    public int Id { get; set; }

    public PaymentMethod PaymentMethod { get; set; }
    public ContractType ContractType => ContractType.DoorSplit;
    public decimal ArtistDoorPercent { get; set; }
}
