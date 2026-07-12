using Concertable.B2B.Concert.Application.Interfaces;

namespace Concertable.B2B.Concert.Application.Renderers;

internal sealed class FlatFeeFingerprintComponent : IContractFingerprintComponent
{
    public string Compose(IContract contract) => $"Fee={TermsFingerprintFormat.Number(((FlatFeeContract)contract).Fee)}";
}
