using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Domain.Entities;

namespace Concertable.B2B.Deal.Application.Strategies;

internal static class DealStrategyFactoryExtensions
{
    extension<TStrategy>(IDealStrategyFactory<TStrategy> factory)
        where TStrategy : class, IDealStrategy
    {
        public TStrategy Create(DealDto deal) => factory.Create(deal.DealType);

        public TStrategy Create(DealEntity entity) => factory.Create(entity.DealType);
    }
}
