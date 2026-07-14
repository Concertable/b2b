using Concertable.B2B.Concert.Application.Interfaces;

namespace Concertable.B2B.Concert.Application.Renderers;

internal sealed class FlatFeeTermsSerializer : IDealTermsSerializer
{
    public string Serialize(IDeal deal) => $"Fee={TermsFingerprintFormat.Number(((FlatFeeDeal)deal).Fee)}";
}
