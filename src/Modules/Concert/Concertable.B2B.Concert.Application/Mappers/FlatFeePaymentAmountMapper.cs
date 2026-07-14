using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Deal.Contracts;

namespace Concertable.B2B.Concert.Application.Mappers;

internal sealed class FlatFeePaymentAmountMapper : IPaymentAmountMapper
{
    public IPaymentAmount ToPaymentAmount(IDeal deal)
    {
        var c = (FlatFeeDeal)deal;
        return new FlatPayment(c.Fee);
    }
}
