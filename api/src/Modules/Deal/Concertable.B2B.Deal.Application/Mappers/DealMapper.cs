using Concertable.B2B.Deal.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace Concertable.B2B.Deal.Application.Mappers;

[Mapper]
internal static partial class DealMapper
{
    [MapperIgnoreSource(nameof(DealEntity.TenantId))]
    [MapDerivedType<FlatFeeDealEntity, FlatFeeDealDto>]
    [MapDerivedType<DoorSplitDealEntity, DoorSplitDealDto>]
    [MapDerivedType<VersusDealEntity, VersusDealDto>]
    [MapDerivedType<VenueHireDealEntity, VenueHireDealDto>]
    public static partial DealDto ToDto(DealEntity entity);

    public static partial IReadOnlyList<DealDto> ToDtos(IEnumerable<DealEntity> entities);
}
