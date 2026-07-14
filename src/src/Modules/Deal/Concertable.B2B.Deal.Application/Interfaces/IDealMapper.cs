using Concertable.B2B.Deal.Domain.Entities;

namespace Concertable.B2B.Deal.Application.Interfaces;

internal interface IDealMapper
{
    IDeal ToDeal(DealEntity entity);
    DealEntity ToEntity(IDeal contract);

    IEnumerable<IDeal> ToDeals(IEnumerable<DealEntity> entities) => entities.Select(ToDeal);
    IEnumerable<DealEntity> ToEntities(IEnumerable<IDeal> contracts) => contracts.Select(ToEntity);
}
