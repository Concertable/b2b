using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Deal.Contracts;

namespace Concertable.B2B.Concert.Application.Mappers;

internal interface IPaymentAmountMapper
{
    IPaymentAmount ToPaymentAmount(IDeal contract);
}
