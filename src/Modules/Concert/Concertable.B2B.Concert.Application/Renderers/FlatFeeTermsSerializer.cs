using Concertable.B2B.Concert.Application.Interfaces;

namespace Concertable.B2B.Concert.Application.Renderers;

internal sealed class FlatFeeTermsSerializer : IContractTermsSerializer
{
    public string Serialize(IContract contract) => $"Fee={TermsFingerprintFormat.Number(((FlatFeeContract)contract).Fee)}";
}
