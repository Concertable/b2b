namespace Concertable.B2B.Deal.Contracts;

public sealed record FlatFeeDealDto : DealDto
{
    public override DealType DealType => DealType.FlatFee;
    public decimal Fee { get; init; }

    public override DealTerms Terms => new FlatFeeTerms(Fee);
}
