namespace Concertable.B2B.Deal.Contracts;

public sealed record DoorSplitDealDto : DealDto
{
    public override DealType DealType => DealType.DoorSplit;
    public decimal ArtistDoorPercent { get; init; }

    public override DealTerms Terms => new DoorSplitTerms(ArtistDoorPercent);

    public override Result<DealEntity, ValidationErrors> ToEntity() =>
        DoorSplitDealEntity.Create(ArtistDoorPercent, PaymentMethod).Map<DealEntity>(entity => entity);
}
