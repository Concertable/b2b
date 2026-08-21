using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Deal.Contracts;

namespace Concertable.B2B.Concert.Application.Mappers;

internal sealed class DoorSplitPaymentAmountMapper : IPaymentAmountMapper
{
    public IPaymentAmount ToPaymentAmount(DealDto deal)
    {
        var c = (DoorSplitDealDto)deal;
        return new DoorSharePayment(c.ArtistDoorPercent);
    }
}
