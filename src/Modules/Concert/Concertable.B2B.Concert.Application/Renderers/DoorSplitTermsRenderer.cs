using Concertable.B2B.Concert.Application.Interfaces;
using static Concertable.B2B.Concert.Application.Renderers.AgreementTermsFormat;

namespace Concertable.B2B.Concert.Application.Renderers;

internal sealed class DoorSplitTermsRenderer : IAgreementTermsRenderer
{
    public string Render(IContract contract)
    {
        var c = (DoorSplitContract)contract;
        return $"The artist receives {Percent(c.ArtistDoorPercent)} of door revenue.";
    }
}
