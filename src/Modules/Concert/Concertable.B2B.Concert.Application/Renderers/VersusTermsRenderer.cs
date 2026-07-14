using Concertable.B2B.Concert.Application.Interfaces;
using static Concertable.B2B.Concert.Application.Renderers.AgreementTermsFormat;

namespace Concertable.B2B.Concert.Application.Renderers;

internal sealed class VersusTermsRenderer : IDealTermsRenderer
{
    public string Render(IDeal contract)
    {
        var c = (VersusDeal)contract;
        return $"The artist receives a guarantee of {Gbp(c.Guarantee)} plus {Percent(c.ArtistDoorPercent)} of door revenue.";
    }
}
