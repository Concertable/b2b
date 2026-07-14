using System.Collections.Frozen;
using Concertable.B2B.Concert.Application.Interfaces;

namespace Concertable.B2B.Concert.Application.Renderers;

internal sealed class DealTermsSerializer : IDealTermsSerializer
{
    private readonly FrozenDictionary<DealType, IDealTermsSerializer> serializers;

    public DealTermsSerializer(
        FlatFeeTermsSerializer flatFee,
        DoorSplitTermsSerializer doorSplit,
        VersusTermsSerializer versus,
        VenueHireTermsSerializer venueHire)
    {
        serializers = new Dictionary<DealType, IDealTermsSerializer>
        {
            [DealType.FlatFee] = flatFee,
            [DealType.DoorSplit] = doorSplit,
            [DealType.Versus] = versus,
            [DealType.VenueHire] = venueHire,
        }.ToFrozenDictionary();
    }

    public string Serialize(IDeal contract) =>
        serializers[contract.ContractType].Serialize(contract);
}
