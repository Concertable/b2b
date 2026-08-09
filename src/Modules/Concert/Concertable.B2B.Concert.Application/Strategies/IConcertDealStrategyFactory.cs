namespace Concertable.B2B.Concert.Application.Strategies;

internal interface IConcertDealStrategyFactory<out TStrategy>
    where TStrategy : class
{
    TStrategy Create(DealType dealType);
}
