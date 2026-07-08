using Concertable.B2B.Concert.Application.Interfaces;
using static System.FormattableString;

namespace Concertable.B2B.Concert.Application.Renderers;

internal sealed class VersusFingerprintComponent : IContractFingerprintComponent
{
    public string Compose(IContract contract)
    {
        var c = (VersusContract)contract;
        return Invariant($"Guarantee={c.Guarantee};ArtistDoorPercent={c.ArtistDoorPercent}");
    }
}
