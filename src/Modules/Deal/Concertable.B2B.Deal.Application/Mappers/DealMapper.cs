using System.Collections.Frozen;
using Concertable.B2B.Deal.Application.Interfaces;
using Concertable.B2B.Deal.Domain.Entities;

namespace Concertable.B2B.Deal.Application.Mappers;

internal sealed class DealMapper : IDealMapper
{
    private readonly FrozenDictionary<DealType, IDealMapper> mappers;

    public DealMapper(
        FlatFeeDealMapper flatFee,
        DoorSplitDealMapper doorSplit,
        VersusDealMapper versus,
        VenueHireDealMapper venueHire)
    {
        mappers = new Dictionary<DealType, IDealMapper>
        {
            [DealType.FlatFee] = flatFee,
            [DealType.DoorSplit] = doorSplit,
            [DealType.Versus] = versus,
            [DealType.VenueHire] = venueHire,
        }.ToFrozenDictionary();
    }

    public IDeal ToDeal(DealEntity entity) =>
        mappers[entity.DealType].ToDeal(entity);

    public DealEntity ToEntity(IDeal deal) =>
        mappers[deal.DealType].ToEntity(deal);
}
