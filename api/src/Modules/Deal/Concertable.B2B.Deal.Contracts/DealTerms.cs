using System.Globalization;
using Concertable.B2B.Deal.Contracts.Enums;

namespace Concertable.B2B.Deal.Contracts;

public abstract record DealTerms
{
    public abstract DealType DealType { get; }

    public abstract string Render();
}

public interface ISettledFromDoorRevenue
{
    decimal ArtistDoorPercent { get; }
}

public sealed record FlatFeeTerms(decimal Fee) : DealTerms
{
    public override DealType DealType => DealType.FlatFee;

    public override string Render() =>
        $"The venue pays the artist a flat fee of {DealTermsFormat.Gbp(Fee)}.";
}

public sealed record VenueHireTerms(decimal HireFee) : DealTerms
{
    public override DealType DealType => DealType.VenueHire;

    public override string Render() =>
        $"The artist pays the venue a hire fee of {DealTermsFormat.Gbp(HireFee)}.";
}

public sealed record DoorSplitTerms(decimal ArtistDoorPercent) : DealTerms, ISettledFromDoorRevenue
{
    public override DealType DealType => DealType.DoorSplit;

    public override string Render() =>
        $"The artist receives {DealTermsFormat.Percent(ArtistDoorPercent)} of door revenue.";
}

public sealed record VersusTerms(decimal Guarantee, decimal ArtistDoorPercent)
    : DealTerms, ISettledFromDoorRevenue
{
    public override DealType DealType => DealType.Versus;

    public override string Render() =>
        $"The artist receives a guarantee of {DealTermsFormat.Gbp(Guarantee)} plus " +
        $"{DealTermsFormat.Percent(ArtistDoorPercent)} of door revenue.";
}

internal static class DealTermsFormat
{
    private static readonly CultureInfo Gb = CultureInfo.GetCultureInfo("en-GB");

    public static string Gbp(decimal amount) => amount.ToString("C", Gb);

    public static string Percent(decimal percent) => $"{percent.ToString("0.##", Gb)}%";
}
