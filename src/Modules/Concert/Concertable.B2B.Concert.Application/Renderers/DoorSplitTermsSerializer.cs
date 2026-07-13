using Concertable.B2B.Concert.Application.Interfaces;

namespace Concertable.B2B.Concert.Application.Renderers;

internal sealed class DoorSplitTermsSerializer : IContractTermsSerializer
{
    public string Serialize(IContract contract) =>
        $"ArtistDoorPercent={TermsFingerprintFormat.Number(((DoorSplitContract)contract).ArtistDoorPercent)}";
}
