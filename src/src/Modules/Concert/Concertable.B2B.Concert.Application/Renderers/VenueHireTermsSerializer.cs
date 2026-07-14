using Concertable.B2B.Concert.Application.Interfaces;

namespace Concertable.B2B.Concert.Application.Renderers;

internal sealed class VenueHireTermsSerializer : IDealTermsSerializer
{
    public string Serialize(IDeal contract) => $"HireFee={TermsFingerprintFormat.Number(((VenueHireDeal)contract).HireFee)}";
}
