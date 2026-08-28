using Concertable.B2B.Deal.Domain.Entities;
using Reunion.Errors;
using Reunion;

namespace Concertable.B2B.Deal.Application.Mappers;

internal interface IDealMapper : IDealStrategy
{
    DealDto ToDeal(DealEntity entity);
    Result<DealEntity, ValidationErrors> ToEntity(DealDto deal);

    IReadOnlyList<DealDto> ToDeals(IEnumerable<DealEntity> entities) => entities.Select(ToDeal).ToList();
}
