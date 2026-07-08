using Concertable.B2B.Concert.Application.Interfaces;
using static Concertable.B2B.Concert.Application.Renderers.AgreementTermsFormat;

namespace Concertable.B2B.Concert.Application.Renderers;

internal sealed class FlatFeeTermsRenderer : IAgreementTermsRenderer
{
    public string Render(IContract contract)
    {
        var c = (FlatFeeContract)contract;
        return $"The venue pays the artist a flat fee of {Gbp(c.Fee)}.";
    }
}
