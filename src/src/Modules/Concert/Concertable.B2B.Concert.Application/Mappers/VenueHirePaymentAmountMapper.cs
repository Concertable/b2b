using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Deal.Contracts;

namespace Concertable.B2B.Concert.Application.Mappers;

internal sealed class VenueHirePaymentAmountMapper : IPaymentAmountMapper
{
    public IPaymentAmount ToPaymentAmount(IDeal contract)
    {
        var c = (VenueHireDeal)contract;
        return new FlatPayment(c.HireFee);
    }
}
