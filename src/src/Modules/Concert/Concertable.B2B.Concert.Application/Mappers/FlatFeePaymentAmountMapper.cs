using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Contract.Contracts;

namespace Concertable.B2B.Concert.Application.Mappers;

internal sealed class FlatFeePaymentAmountMapper : IPaymentAmountMapper
{
    public IPaymentAmount ToPaymentAmount(IContract contract)
    {
        var c = (FlatFeeContract)contract;
        return new FlatPayment(c.Fee);
    }
}
