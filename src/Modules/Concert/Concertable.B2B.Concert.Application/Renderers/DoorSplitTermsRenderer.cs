using Concertable.B2B.Concert.Application.Interfaces;
using static Concertable.B2B.Concert.Application.Renderers.AgreementTermsFormat;

namespace Concertable.B2B.Concert.Application.Renderers;

internal sealed class DoorSplitTermsRenderer : IDealTermsRenderer
{
    public string Render(IDeal contract)
    {
        var c = (DoorSplitDeal)contract;
        return $"The artist receives {Percent(c.ArtistDoorPercent)} of door revenue.";
    }
}
