using Concertable.B2B.Concert.Application.Interfaces;
using static Concertable.B2B.Concert.Application.Renderers.DealTermsFormat;

namespace Concertable.B2B.Concert.Application.Renderers;

internal sealed class FlatFeeTermsRenderer : IDealTermsRenderer
{
    public string Render(IDeal deal)
    {
        var c = (FlatFeeDeal)deal;
        return $"The venue pays the artist a flat fee of {Gbp(c.Fee)}.";
    }
}
