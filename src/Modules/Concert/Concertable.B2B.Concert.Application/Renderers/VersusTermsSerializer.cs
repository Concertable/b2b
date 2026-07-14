using Concertable.B2B.Concert.Application.Interfaces;

namespace Concertable.B2B.Concert.Application.Renderers;

internal sealed class VersusTermsSerializer : IDealTermsSerializer
{
    public string Serialize(IDeal deal)
    {
        var c = (VersusDeal)deal;
        return $"Guarantee={TermsFingerprintFormat.Number(c.Guarantee)};ArtistDoorPercent={TermsFingerprintFormat.Number(c.ArtistDoorPercent)}";
    }
}
