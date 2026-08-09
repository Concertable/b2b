using Concertable.B2B.Concert.Application.Interfaces;
using static Concertable.B2B.Concert.Application.Renderers.DealTermsFormat;

namespace Concertable.B2B.Concert.Application.Renderers;

internal sealed class FlatFeeDealTerms : IDealTerms
{
    public string Render(IDeal deal)
    {
        var terms = (FlatFeeDeal)deal;
        return $"The venue pays the artist a flat fee of {Gbp(terms.Fee)}.";
    }

    public string Serialize(IDeal deal) =>
        $"Fee={TermsFingerprintFormat.Number(((FlatFeeDeal)deal).Fee)}";
}
