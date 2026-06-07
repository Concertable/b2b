using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Contract.Contracts;

namespace Concertable.B2B.Concert.Application.Mappers;

internal interface IPaymentAmountMapper
{
    IPaymentAmount ToPaymentAmount(IContract contract);
}
