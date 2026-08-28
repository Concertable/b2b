using Concertable.B2B.Deal.Application.Strategies;
using Concertable.B2B.Deal.Domain.Entities;
using Reunion.Errors;
using Reunion;

namespace Concertable.B2B.Deal.Application.Mappers;

internal sealed class DealMapper : IDealMapper
{
    private readonly IDealStrategyFactory<IDealMapper> strategies;

    public DealMapper(IDealStrategyFactory<IDealMapper> strategies)
    {
        this.strategies = strategies;
    }

    public DealDto ToDeal(DealEntity entity) =>
        strategies.Create(entity).ToDeal(entity);

    public Result<DealEntity, ValidationErrors> ToEntity(DealDto deal) =>
        strategies.Create(deal).ToEntity(deal);
}
