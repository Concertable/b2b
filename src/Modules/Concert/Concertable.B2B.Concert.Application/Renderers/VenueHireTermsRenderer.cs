using Concertable.B2B.Concert.Application.Interfaces;
using static Concertable.B2B.Concert.Application.Renderers.DealTermsFormat;

namespace Concertable.B2B.Concert.Application.Renderers;

internal sealed class VenueHireTermsRenderer : IDealTermsRenderer
{
    public string Render(IDeal deal)
    {
        var c = (VenueHireDeal)deal;
        return $"The artist pays the venue a hire fee of {Gbp(c.HireFee)}.";
    }
}
