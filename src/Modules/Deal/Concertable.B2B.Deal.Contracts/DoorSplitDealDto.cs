namespace Concertable.B2B.Deal.Contracts;

public sealed record DoorSplitDealDto : DealDto
{
    public override DealType DealType => DealType.DoorSplit;
    public decimal ArtistDoorPercent { get; init; }
}
