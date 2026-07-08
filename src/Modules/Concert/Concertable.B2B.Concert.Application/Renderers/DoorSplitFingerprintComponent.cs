using Concertable.B2B.Concert.Application.Interfaces;
using static System.FormattableString;

namespace Concertable.B2B.Concert.Application.Renderers;

internal sealed class DoorSplitFingerprintComponent : IContractFingerprintComponent
{
    public string Compose(IContract contract) =>
        Invariant($"ArtistDoorPercent={((DoorSplitContract)contract).ArtistDoorPercent}");
}
