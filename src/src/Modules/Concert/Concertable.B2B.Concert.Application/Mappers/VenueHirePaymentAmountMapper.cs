using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Deal.Contracts;

namespace Concertable.B2B.Concert.Application.Mappers;

internal sealed class VenueHirePaymentAmountMapper : IPaymentAmountMapper
{
    public IPaymentAmount ToPaymentAmount(DealDto deal)
    {
        var c = (VenueHireDealDto)deal;
        return new FlatPayment(c.HireFee);
    }
}
