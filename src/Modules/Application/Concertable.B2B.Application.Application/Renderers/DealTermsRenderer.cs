using System.Globalization;
using Concertable.B2B.Application.Application.Interfaces;

namespace Concertable.B2B.Application.Application.Renderers;

internal sealed class DealTermsRenderer : IDealTermsRenderer
{
    private readonly IDealTypeStrategyFactory<IDealTerms> terms;

    public DealTermsRenderer(IDealTypeStrategyFactory<IDealTerms> terms)
    {
        this.terms = terms;
    }

    public string Render(DealDto deal) => terms.Create(deal.DealType).Render(deal);
}

internal sealed class FlatFeeDealTerms : IDealTerms
{
    public string Render(DealDto deal) =>
        $"The venue pays the artist a flat fee of {DealTermsFormat.Gbp(((FlatFeeDealDto)deal).Fee)}.";

}

internal sealed class DoorSplitDealTerms : IDealTerms
{
    public string Render(DealDto deal) =>
        $"The artist receives {DealTermsFormat.Percent(((DoorSplitDealDto)deal).ArtistDoorPercent)} of door revenue.";

}

internal sealed class VersusDealTerms : IDealTerms
{
    public string Render(DealDto deal)
    {
        var versus = (VersusDealDto)deal;
        return $"The artist receives a guarantee of {DealTermsFormat.Gbp(versus.Guarantee)} plus {DealTermsFormat.Percent(versus.ArtistDoorPercent)} of door revenue.";
    }

}

internal sealed class VenueHireDealTerms : IDealTerms
{
    public string Render(DealDto deal) =>
        $"The artist pays the venue a hire fee of {DealTermsFormat.Gbp(((VenueHireDealDto)deal).HireFee)}.";

}

internal static class DealTermsFormat
{
    private static readonly CultureInfo Gb = CultureInfo.GetCultureInfo("en-GB");

    public static string Gbp(decimal amount) => amount.ToString("C", Gb);
    public static string Percent(decimal percent) => $"{percent.ToString("0.##", Gb)}%";
}
