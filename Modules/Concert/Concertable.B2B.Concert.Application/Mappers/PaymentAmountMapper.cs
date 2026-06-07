using System.Collections.Frozen;
using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Contract.Contracts;

namespace Concertable.B2B.Concert.Application.Mappers;

internal sealed class PaymentAmountMapper : IPaymentAmountMapper
{
    private readonly FrozenDictionary<ContractType, IPaymentAmountMapper> mappers;

    public PaymentAmountMapper(
        FlatFeePaymentAmountMapper flatFee,
        DoorSplitPaymentAmountMapper doorSplit,
        VersusPaymentAmountMapper versus,
        VenueHirePaymentAmountMapper venueHire)
    {
        mappers = new Dictionary<ContractType, IPaymentAmountMapper>
        {
            [ContractType.FlatFee] = flatFee,
            [ContractType.DoorSplit] = doorSplit,
            [ContractType.Versus] = versus,
            [ContractType.VenueHire] = venueHire,
        }.ToFrozenDictionary();
    }

    public IPaymentAmount ToPaymentAmount(IContract contract) =>
        mappers[contract.ContractType].ToPaymentAmount(contract);
}
