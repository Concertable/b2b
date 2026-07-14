using Concertable.B2B.Concert.Application.Interfaces;

namespace Concertable.B2B.Concert.Application.Renderers;

internal sealed class DoorSplitTermsSerializer : IDealTermsSerializer
{
    public string Serialize(IDeal deal) =>
        $"ArtistDoorPercent={TermsFingerprintFormat.Number(((DoorSplitDeal)deal).ArtistDoorPercent)}";
}
