using System.Collections.Frozen;
using Concertable.B2B.Concert.Application.Interfaces;

namespace Concertable.B2B.Concert.Application.Renderers;

internal sealed class ContractTermsSerializer : IContractTermsSerializer
{
    private readonly FrozenDictionary<ContractType, IContractTermsSerializer> serializers;

    public ContractTermsSerializer(
        FlatFeeTermsSerializer flatFee,
        DoorSplitTermsSerializer doorSplit,
        VersusTermsSerializer versus,
        VenueHireTermsSerializer venueHire)
    {
        serializers = new Dictionary<ContractType, IContractTermsSerializer>
        {
            [ContractType.FlatFee] = flatFee,
            [ContractType.DoorSplit] = doorSplit,
            [ContractType.Versus] = versus,
            [ContractType.VenueHire] = venueHire,
        }.ToFrozenDictionary();
    }

    public string Serialize(IContract contract) =>
        serializers[contract.ContractType].Serialize(contract);
}
