using Concertable.B2B.Concert.Application.Interfaces;
using static Concertable.B2B.Concert.Application.Renderers.DealTermsFormat;

namespace Concertable.B2B.Concert.Application.Renderers;

internal sealed class VenueHireDealTerms : IDealTerms
{
    public string Render(DealDto deal)
    {
        var terms = (VenueHireDealDto)deal;
        return $"The artist pays the venue a hire fee of {Gbp(terms.HireFee)}.";
    }

    public string Serialize(DealDto deal) =>
        $"HireFee={TermsFingerprintFormat.Number(((VenueHireDealDto)deal).HireFee)}";
}
