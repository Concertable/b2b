using Concertable.B2B.Concert.Application.Interfaces;
using static Concertable.B2B.Concert.Application.Renderers.DealTermsFormat;

namespace Concertable.B2B.Concert.Application.Renderers;

internal sealed class DoorSplitTermsRenderer : IDealTermsRenderer
{
    public string Render(IDeal deal)
    {
        var c = (DoorSplitDeal)deal;
        return $"The artist receives {Percent(c.ArtistDoorPercent)} of door revenue.";
    }
}
