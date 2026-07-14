using System.Collections.Frozen;
using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Deal.Contracts;

namespace Concertable.B2B.Concert.Application.Mappers;

internal sealed class PaymentAmountMapper : IPaymentAmountMapper
{
    private readonly FrozenDictionary<DealType, IPaymentAmountMapper> mappers;

    public PaymentAmountMapper(
        FlatFeePaymentAmountMapper flatFee,
        DoorSplitPaymentAmountMapper doorSplit,
        VersusPaymentAmountMapper versus,
        VenueHirePaymentAmountMapper venueHire)
    {
        mappers = new Dictionary<DealType, IPaymentAmountMapper>
        {
            [DealType.FlatFee] = flatFee,
            [DealType.DoorSplit] = doorSplit,
            [DealType.Versus] = versus,
            [DealType.VenueHire] = venueHire,
        }.ToFrozenDictionary();
    }

    public IPaymentAmount ToPaymentAmount(IDeal contract) =>
        mappers[contract.ContractType].ToPaymentAmount(contract);
}
