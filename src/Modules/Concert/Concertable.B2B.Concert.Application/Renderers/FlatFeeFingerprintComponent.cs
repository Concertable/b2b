using Concertable.B2B.Concert.Application.Interfaces;
using static System.FormattableString;

namespace Concertable.B2B.Concert.Application.Renderers;

internal sealed class FlatFeeFingerprintComponent : IContractFingerprintComponent
{
    public string Compose(IContract contract) => Invariant($"Fee={((FlatFeeContract)contract).Fee}");
}
