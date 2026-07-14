using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Deal.Contracts;

namespace Concertable.B2B.Concert.Application.Mappers;

internal sealed class FlatFeePaymentAmountMapper : IPaymentAmountMapper
{
    public IPaymentAmount ToPaymentAmount(IDeal contract)
    {
        var c = (FlatFeeDeal)contract;
        return new FlatPayment(c.Fee);
    }
}
