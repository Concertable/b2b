using Concertable.B2B.Concert.Application.Interfaces;
using static Concertable.B2B.Concert.Application.Renderers.DealTermsFormat;

namespace Concertable.B2B.Concert.Application.Renderers;

internal sealed class FlatFeeDealTerms : IDealTerms
{
    public string Render(DealDto deal)
    {
        var terms = (FlatFeeDealDto)deal;
        return $"The venue pays the artist a flat fee of {Gbp(terms.Fee)}.";
    }

    public string Serialize(DealDto deal) =>
        $"Fee={TermsFingerprintFormat.Number(((FlatFeeDealDto)deal).Fee)}";
}
