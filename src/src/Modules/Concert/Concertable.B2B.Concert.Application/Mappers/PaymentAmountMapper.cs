using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Concert.Application.Strategies;
using Concertable.B2B.Deal.Contracts;

namespace Concertable.B2B.Concert.Application.Mappers;

internal sealed class PaymentAmountMapper : IPaymentAmountMapper
{
    private readonly IConcertDealStrategyFactory<IPaymentAmountMapper> mappers;

    public PaymentAmountMapper(IConcertDealStrategyFactory<IPaymentAmountMapper> mappers)
    {
        this.mappers = mappers;
    }

    public IPaymentAmount ToPaymentAmount(DealDto deal) =>
        mappers.Create(deal.DealType).ToPaymentAmount(deal);
}
