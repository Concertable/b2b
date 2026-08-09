namespace Concertable.B2B.Deal.Application.Strategies;

internal interface IDealStrategyFactory<out TStrategy>
    where TStrategy : class
{
    TStrategy Create(DealType dealType);
}
