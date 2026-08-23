namespace Concertable.B2B.Application.Application.Strategies;

internal interface IApplicationDealStrategyFactory<TStrategy>
    where TStrategy : class
{
    TStrategy Create(DealType dealType);
}
