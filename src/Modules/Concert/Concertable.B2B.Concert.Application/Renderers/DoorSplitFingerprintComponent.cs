using Concertable.B2B.Concert.Application.Interfaces;

namespace Concertable.B2B.Concert.Application.Renderers;

internal sealed class DoorSplitFingerprintComponent : IContractFingerprintComponent
{
    public string Compose(IContract contract) =>
        $"ArtistDoorPercent={TermsFingerprintFormat.Number(((DoorSplitContract)contract).ArtistDoorPercent)}";
}
