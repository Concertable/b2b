using System.Collections.Frozen;
using Concertable.B2B.Concert.Application.Interfaces;

namespace Concertable.B2B.Concert.Application.Renderers;

internal sealed class DealTermsRenderer : IDealTermsRenderer
{
    private readonly FrozenDictionary<DealType, IDealTermsRenderer> renderers;

    public DealTermsRenderer(
        FlatFeeTermsRenderer flatFee,
        DoorSplitTermsRenderer doorSplit,
        VersusTermsRenderer versus,
        VenueHireTermsRenderer venueHire)
    {
        renderers = new Dictionary<DealType, IDealTermsRenderer>
        {
            [DealType.FlatFee] = flatFee,
            [DealType.DoorSplit] = doorSplit,
            [DealType.Versus] = versus,
            [DealType.VenueHire] = venueHire,
        }.ToFrozenDictionary();
    }

    public string Render(IDeal contract) =>
        renderers[contract.ContractType].Render(contract);
}
