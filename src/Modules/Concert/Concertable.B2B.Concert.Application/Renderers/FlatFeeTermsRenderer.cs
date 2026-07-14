using Concertable.B2B.Concert.Application.Interfaces;
using static Concertable.B2B.Concert.Application.Renderers.AgreementTermsFormat;

namespace Concertable.B2B.Concert.Application.Renderers;

internal sealed class FlatFeeTermsRenderer : IDealTermsRenderer
{
    public string Render(IDeal contract)
    {
        var c = (FlatFeeDeal)contract;
        return $"The venue pays the artist a flat fee of {Gbp(c.Fee)}.";
    }
}
