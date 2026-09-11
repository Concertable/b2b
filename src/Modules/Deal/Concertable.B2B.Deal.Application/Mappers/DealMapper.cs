using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Domain.Entities;
using Reunion.Errors;
using Reunion;

namespace Concertable.B2B.Deal.Application.Mappers;

internal sealed class DealMapper : IDealMapper
{
    private readonly IDealStrategyFactory<IDealMapper> factory;

    public DealMapper(IDealStrategyFactory<IDealMapper> factory)
    {
        this.factory = factory;
    }

    public DealDto ToDeal(DealEntity entity) =>
        factory.Create(entity.DealType).ToDeal(entity);

    public Result<DealEntity, ValidationErrors> ToEntity(DealDto deal) =>
        factory.Create(deal.DealType).ToEntity(deal);
}
