using Concertable.B2B.Concert.Application.Interfaces;
using static Concertable.B2B.Concert.Application.Renderers.DealTermsFormat;

namespace Concertable.B2B.Concert.Application.Renderers;

internal sealed class DoorSplitDealTerms : IDealTerms
{
    public string Render(DealDto deal)
    {
        var terms = (DoorSplitDealDto)deal;
        return $"The artist receives {Percent(terms.ArtistDoorPercent)} of door revenue.";
    }

    public string Serialize(DealDto deal) =>
        $"ArtistDoorPercent={TermsFingerprintFormat.Number(((DoorSplitDealDto)deal).ArtistDoorPercent)}";
}
