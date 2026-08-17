using System.Globalization;
using Concertable.B2B.Application.Application.Interfaces;

namespace Concertable.B2B.Application.Application.Renderers;

internal sealed class DealTermsRenderer(IStepResolver<IDealTerms> terms) : IDealTermsRenderer
{
    public string Render(IDeal deal) => terms.Resolve(deal.DealType).Render(deal);
}

internal sealed class FlatFeeDealTerms : IDealTerms
{
    public string Render(IDeal deal) =>
        $"The venue pays the artist a flat fee of {DealTermsFormat.Gbp(((FlatFeeDeal)deal).Fee)}.";

    public string Serialize(IDeal deal) =>
        $"Fee={TermsFingerprintFormat.Number(((FlatFeeDeal)deal).Fee)}";
}

internal sealed class DoorSplitDealTerms : IDealTerms
{
    public string Render(IDeal deal) =>
        $"The artist receives {DealTermsFormat.Percent(((DoorSplitDeal)deal).ArtistDoorPercent)} of door revenue.";

    public string Serialize(IDeal deal) =>
        $"ArtistDoorPercent={TermsFingerprintFormat.Number(((DoorSplitDeal)deal).ArtistDoorPercent)}";
}

internal sealed class VersusDealTerms : IDealTerms
{
    public string Render(IDeal deal)
    {
        var versus = (VersusDeal)deal;
        return $"The artist receives a guarantee of {DealTermsFormat.Gbp(versus.Guarantee)} plus {DealTermsFormat.Percent(versus.ArtistDoorPercent)} of door revenue.";
    }

    public string Serialize(IDeal deal)
    {
        var versus = (VersusDeal)deal;
        return $"Guarantee={TermsFingerprintFormat.Number(versus.Guarantee)};ArtistDoorPercent={TermsFingerprintFormat.Number(versus.ArtistDoorPercent)}";
    }
}

internal sealed class VenueHireDealTerms : IDealTerms
{
    public string Render(IDeal deal) =>
        $"The artist pays the venue a hire fee of {DealTermsFormat.Gbp(((VenueHireDeal)deal).HireFee)}.";

    public string Serialize(IDeal deal) =>
        $"HireFee={TermsFingerprintFormat.Number(((VenueHireDeal)deal).HireFee)}";
}

internal static class DealTermsFormat
{
    private static readonly CultureInfo Gb = CultureInfo.GetCultureInfo("en-GB");

    public static string Gbp(decimal amount) => amount.ToString("C", Gb);
    public static string Percent(decimal percent) => $"{percent.ToString("0.##", Gb)}%";
}
