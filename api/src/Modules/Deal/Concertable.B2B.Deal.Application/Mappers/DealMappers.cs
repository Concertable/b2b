using Concertable.B2B.Deal.Domain.Entities;
using Reunion;

namespace Concertable.B2B.Deal.Application.Mappers;

internal static class DealMappers
{
    extension(DealDto deal)
    {
        public Result<DealEntity, ValidationErrors> ToEntity() => deal switch
        {
            FlatFeeDealDto d => FlatFeeDealEntity
                .Create(d.Fee, d.PaymentMethod)
                .Map<DealEntity>(entity => entity),
            DoorSplitDealDto d => DoorSplitDealEntity
                .Create(d.ArtistDoorPercent, d.PaymentMethod)
                .Map<DealEntity>(entity => entity),
            VersusDealDto d => VersusDealEntity
                .Create(d.Guarantee, d.ArtistDoorPercent, d.PaymentMethod)
                .Map<DealEntity>(entity => entity),
            VenueHireDealDto d => VenueHireDealEntity
                .Create(d.HireFee, d.PaymentMethod)
                .Map<DealEntity>(entity => entity),
            _ => throw new ArgumentOutOfRangeException(nameof(deal), deal.DealType, null)
        };
    }
}
