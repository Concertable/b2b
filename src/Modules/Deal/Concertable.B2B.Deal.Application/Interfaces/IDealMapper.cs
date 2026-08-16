using Concertable.B2B.Deal.Domain.Entities;
using Reunion.Errors;
using Reunion;

namespace Concertable.B2B.Deal.Application.Interfaces;

internal interface IDealMapper
{
    IDeal ToDeal(DealEntity entity);
    Result<DealEntity, ValidationErrors> ToEntity(IDeal deal);

    IReadOnlyList<IDeal> ToDeals(IEnumerable<DealEntity> entities) => entities.Select(ToDeal).ToList();
}
