using Concertable.B2B.Concert.Application.Interfaces;

namespace Concertable.B2B.Concert.Application.Renderers;

internal sealed class VersusFingerprintComponent : IContractFingerprintComponent
{
    public string Compose(IContract contract)
    {
        var c = (VersusContract)contract;
        return $"Guarantee={TermsFingerprintFormat.Number(c.Guarantee)};ArtistDoorPercent={TermsFingerprintFormat.Number(c.ArtistDoorPercent)}";
    }
}
