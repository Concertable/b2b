using Concertable.B2B.Concert.Application.Interfaces;
using static Concertable.B2B.Concert.Application.Renderers.AgreementTermsFormat;

namespace Concertable.B2B.Concert.Application.Renderers;

internal sealed class VersusTermsRenderer : IAgreementTermsRenderer
{
    public string Render(IContract contract)
    {
        var c = (VersusContract)contract;
        return $"The artist receives a guarantee of {Gbp(c.Guarantee)} plus {Percent(c.ArtistDoorPercent)} of door revenue.";
    }
}
