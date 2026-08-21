using Concertable.B2B.Concert.Application.Interfaces;
using static Concertable.B2B.Concert.Application.Renderers.DealTermsFormat;

namespace Concertable.B2B.Concert.Application.Renderers;

internal sealed class VersusDealTerms : IDealTerms
{
    public string Render(DealDto deal)
    {
        var terms = (VersusDealDto)deal;
        return $"The artist receives a guarantee of {Gbp(terms.Guarantee)} plus {Percent(terms.ArtistDoorPercent)} of door revenue.";
    }

    public string Serialize(DealDto deal)
    {
        var terms = (VersusDealDto)deal;
        return $"Guarantee={TermsFingerprintFormat.Number(terms.Guarantee)};ArtistDoorPercent={TermsFingerprintFormat.Number(terms.ArtistDoorPercent)}";
    }
}
