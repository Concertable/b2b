using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Contract.Contracts;

namespace Concertable.B2B.Concert.Application.Mappers;

internal sealed class DoorSplitPaymentAmountMapper : IPaymentAmountMapper
{
    public IPaymentAmount ToPaymentAmount(IContract contract)
    {
        var c = (DoorSplitContract)contract;
        return new DoorSharePayment(c.ArtistDoorPercent);
    }
}
