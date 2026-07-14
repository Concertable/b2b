using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Deal.Contracts;

namespace Concertable.B2B.Concert.Application.Mappers;

internal sealed class DoorSplitPaymentAmountMapper : IPaymentAmountMapper
{
    public IPaymentAmount ToPaymentAmount(IDeal deal)
    {
        var c = (DoorSplitDeal)deal;
        return new DoorSharePayment(c.ArtistDoorPercent);
    }
}
