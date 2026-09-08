namespace Concertable.B2B.Deal.Contracts;

public sealed record VersusDealDto : DealDto
{
    public override DealType DealType => DealType.Versus;
    public decimal Guarantee { get; init; }
    public decimal ArtistDoorPercent { get; init; }

    public override DealTerms Terms =>
        new VersusTerms(Guarantee, ArtistDoorPercent);

    public override Result<DealEntity, ValidationErrors> ToEntity() =>
        VersusDealEntity.Create(Guarantee, ArtistDoorPercent, PaymentMethod).Map<DealEntity>(entity => entity);
}
