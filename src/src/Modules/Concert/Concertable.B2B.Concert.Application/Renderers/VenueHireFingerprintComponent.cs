using Concertable.B2B.Concert.Application.Interfaces;
using static System.FormattableString;

namespace Concertable.B2B.Concert.Application.Renderers;

internal sealed class VenueHireFingerprintComponent : IContractFingerprintComponent
{
    public string Compose(IContract contract) => Invariant($"HireFee={((VenueHireContract)contract).HireFee}");
}
