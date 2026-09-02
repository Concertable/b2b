using Concertable.B2B.Deal.Contracts.Enums;

namespace Concertable.B2B.Deal.Contracts;

public abstract record DealTerms
{
    public abstract DealType DealType { get; }
}

public interface ISettledFromDoorRevenue
{
    decimal ArtistDoorPercent { get; }
    string PaymentMethodId { get; }
}

public sealed record FlatFeeTerms(decimal Fee) : DealTerms
{
    public override DealType DealType => DealType.FlatFee;
}

public sealed record VenueHireTerms(decimal HireFee) : DealTerms
{
    public override DealType DealType => DealType.VenueHire;
}

public sealed record DoorSplitTerms(decimal ArtistDoorPercent, string PaymentMethodId)
    : DealTerms, ISettledFromDoorRevenue
{
    public override DealType DealType => DealType.DoorSplit;
}

public sealed record VersusTerms(decimal Guarantee, decimal ArtistDoorPercent, string PaymentMethodId)
    : DealTerms, ISettledFromDoorRevenue
{
    public override DealType DealType => DealType.Versus;
}
