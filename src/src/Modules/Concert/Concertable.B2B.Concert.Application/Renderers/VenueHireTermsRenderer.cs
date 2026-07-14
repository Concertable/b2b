using Concertable.B2B.Concert.Application.Interfaces;
using static Concertable.B2B.Concert.Application.Renderers.AgreementTermsFormat;

namespace Concertable.B2B.Concert.Application.Renderers;

internal sealed class VenueHireTermsRenderer : IDealTermsRenderer
{
    public string Render(IDeal contract)
    {
        var c = (VenueHireDeal)contract;
        return $"The artist pays the venue a hire fee of {Gbp(c.HireFee)}.";
    }
}
