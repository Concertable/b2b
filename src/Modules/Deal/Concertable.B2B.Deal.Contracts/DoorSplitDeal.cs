namespace Concertable.B2B.Deal.Contracts;

public sealed record DoorSplitDeal : IDeal
{
    public int Id { get; set; }

    public PaymentMethod PaymentMethod { get; set; }
    public DealType ContractType => DealType.DoorSplit;
    public decimal ArtistDoorPercent { get; set; }
}
