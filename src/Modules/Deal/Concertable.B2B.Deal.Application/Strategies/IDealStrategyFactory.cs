using Concertable.B2B.Deal.Domain.Entities;

namespace Concertable.B2B.Deal.Application.Strategies;

internal interface IDealStrategy;

internal interface IDealStrategyFactory<TStrategy>
    where TStrategy : class, IDealStrategy
{
    TStrategy Create(DealDto deal);
    TStrategy Create(DealEntity entity);
}
