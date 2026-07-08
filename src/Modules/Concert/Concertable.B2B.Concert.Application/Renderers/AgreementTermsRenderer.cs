using System.Collections.Frozen;
using Concertable.B2B.Concert.Application.Interfaces;

namespace Concertable.B2B.Concert.Application.Renderers;

internal sealed class AgreementTermsRenderer : IAgreementTermsRenderer
{
    private readonly FrozenDictionary<ContractType, IAgreementTermsRenderer> renderers;

    public AgreementTermsRenderer(
        FlatFeeTermsRenderer flatFee,
        DoorSplitTermsRenderer doorSplit,
        VersusTermsRenderer versus,
        VenueHireTermsRenderer venueHire)
    {
        renderers = new Dictionary<ContractType, IAgreementTermsRenderer>
        {
            [ContractType.FlatFee] = flatFee,
            [ContractType.DoorSplit] = doorSplit,
            [ContractType.Versus] = versus,
            [ContractType.VenueHire] = venueHire,
        }.ToFrozenDictionary();
    }

    public string Render(IContract contract) =>
        renderers[contract.ContractType].Render(contract);
}
