using Concertable.B2B.Concert.Application.Interfaces;

namespace Concertable.B2B.Concert.Application.Renderers;

internal sealed class VenueHireTermsSerializer : IContractTermsSerializer
{
    public string Serialize(IContract contract) => $"HireFee={TermsFingerprintFormat.Number(((VenueHireContract)contract).HireFee)}";
}
