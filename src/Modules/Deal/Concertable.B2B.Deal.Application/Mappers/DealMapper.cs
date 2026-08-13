using Concertable.B2B.Deal.Application.Interfaces;
using Concertable.B2B.Deal.Application.Strategies;
using Concertable.B2B.Deal.Domain.Entities;

namespace Concertable.B2B.Deal.Application.Mappers;

internal sealed class DealMapper : IDealMapper
{
    private readonly IDealStrategyFactory<IDealMapper> strategies;

    public DealMapper(IDealStrategyFactory<IDealMapper> strategies)
    {
        this.strategies = strategies;
    }

    public IDeal ToDeal(DealEntity entity) =>
        strategies.Create(entity.DealType).ToDeal(entity);

    public DealEntity ToEntity(IDeal deal) =>
        strategies.Create(deal.DealType).ToEntity(deal);
}
