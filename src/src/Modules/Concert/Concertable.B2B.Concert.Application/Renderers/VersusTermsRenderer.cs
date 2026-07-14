using Concertable.B2B.Concert.Application.Interfaces;
using static Concertable.B2B.Concert.Application.Renderers.DealTermsFormat;

namespace Concertable.B2B.Concert.Application.Renderers;

internal sealed class VersusTermsRenderer : IDealTermsRenderer
{
    public string Render(IDeal deal)
    {
        var c = (VersusDeal)deal;
        return $"The artist receives a guarantee of {Gbp(c.Guarantee)} plus {Percent(c.ArtistDoorPercent)} of door revenue.";
    }
}
