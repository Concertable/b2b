namespace Concertable.B2B.Deal.Contracts;

public sealed record VenueHireDealDto : DealDto
{
    public override DealType DealType => DealType.VenueHire;
    public decimal HireFee { get; init; }

    public override DealTerms Terms => new VenueHireTerms(HireFee);
}
