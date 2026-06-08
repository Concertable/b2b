using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Contract.Contracts;

namespace Concertable.B2B.Concert.Application.Mappers;

internal sealed class VenueHirePaymentAmountMapper : IPaymentAmountMapper
{
    public IPaymentAmount ToPaymentAmount(IContract contract)
    {
        var c = (VenueHireContract)contract;
        return new FlatPayment(c.HireFee);
    }
}
