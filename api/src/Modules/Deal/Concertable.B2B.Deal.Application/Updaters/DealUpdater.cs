using Concertable.B2B.Deal.Domain.Entities;
using Reunion;

namespace Concertable.B2B.Deal.Application.Updaters;

internal abstract class DealUpdater
{
    private static readonly DealUpdater FlatFee = new FlatFeeUpdater();
    private static readonly DealUpdater DoorSplit = new DoorSplitUpdater();
    private static readonly DealUpdater Versus = new VersusUpdater();
    private static readonly DealUpdater VenueHire = new VenueHireUpdater();

    public static UnitResult<ValidationErrors> Update(DealEntity entity, DealDto deal)
    {
        if (entity.DealType != deal.DealType)
            return new ValidationErrors([
                new(nameof(deal.DealType), $"A {deal.DealType} deal cannot update a {entity.DealType} deal.")
            ]);

        return For(deal.DealType).Apply(entity, deal);
    }

    protected abstract UnitResult<ValidationErrors> Apply(DealEntity entity, DealDto deal);

    private static DealUpdater For(DealType dealType) => dealType switch
    {
        DealType.FlatFee => FlatFee,
        DealType.DoorSplit => DoorSplit,
        DealType.Versus => Versus,
        DealType.VenueHire => VenueHire,
        _ => throw new ArgumentOutOfRangeException(nameof(dealType), dealType, null)
    };

    private sealed class FlatFeeUpdater : DealUpdater<FlatFeeDealEntity, FlatFeeDealDto>
    {
        protected override UnitResult<ValidationErrors> Apply(FlatFeeDealEntity entity, FlatFeeDealDto deal) =>
            entity.Update(deal.Fee, deal.PaymentMethod);
    }

    private sealed class DoorSplitUpdater : DealUpdater<DoorSplitDealEntity, DoorSplitDealDto>
    {
        protected override UnitResult<ValidationErrors> Apply(DoorSplitDealEntity entity, DoorSplitDealDto deal) =>
            entity.Update(deal.ArtistDoorPercent, deal.PaymentMethod);
    }

    private sealed class VersusUpdater : DealUpdater<VersusDealEntity, VersusDealDto>
    {
        protected override UnitResult<ValidationErrors> Apply(VersusDealEntity entity, VersusDealDto deal) =>
            entity.Update(deal.Guarantee, deal.ArtistDoorPercent, deal.PaymentMethod);
    }

    private sealed class VenueHireUpdater : DealUpdater<VenueHireDealEntity, VenueHireDealDto>
    {
        protected override UnitResult<ValidationErrors> Apply(VenueHireDealEntity entity, VenueHireDealDto deal) =>
            entity.Update(deal.HireFee, deal.PaymentMethod);
    }
}

internal abstract class DealUpdater<TEntity, TDto> : DealUpdater
    where TEntity : DealEntity
    where TDto : DealDto
{
    protected sealed override UnitResult<ValidationErrors> Apply(DealEntity entity, DealDto deal) =>
        Apply((TEntity)entity, (TDto)deal);

    protected abstract UnitResult<ValidationErrors> Apply(TEntity entity, TDto deal);
}
