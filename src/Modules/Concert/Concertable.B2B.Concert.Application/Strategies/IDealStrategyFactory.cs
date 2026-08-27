namespace Concertable.B2B.Concert.Application.Strategies;

internal interface IDealStrategyFactory<TStrategy>
    where TStrategy : class
{
    TStrategy Create(DealType dealType);
}
