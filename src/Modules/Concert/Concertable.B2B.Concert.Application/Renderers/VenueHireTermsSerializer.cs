using Concertable.B2B.Concert.Application.Interfaces;

namespace Concertable.B2B.Concert.Application.Renderers;

internal sealed class VenueHireTermsSerializer : IDealTermsSerializer
{
    public string Serialize(IDeal deal) => $"HireFee={TermsFingerprintFormat.Number(((VenueHireDeal)deal).HireFee)}";
}
