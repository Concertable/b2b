using Concertable.B2B.Concert.Application.Interfaces;
using static Concertable.B2B.Concert.Application.Renderers.DealTermsFormat;

namespace Concertable.B2B.Concert.Application.Renderers;

internal sealed class VenueHireDealTerms : IDealTerms
{
    public string Render(IDeal deal)
    {
        var terms = (VenueHireDeal)deal;
        return $"The artist pays the venue a hire fee of {Gbp(terms.HireFee)}.";
    }

    public string Serialize(IDeal deal) =>
        $"HireFee={TermsFingerprintFormat.Number(((VenueHireDeal)deal).HireFee)}";
}
