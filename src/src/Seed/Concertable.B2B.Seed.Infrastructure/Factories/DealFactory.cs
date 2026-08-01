using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Domain.Entities;
using Concertable.Kernel.Errors;
using Concertable.Kernel.Functional;
using static Concertable.Seed.Identity.Extensions.EntityReflectionExtensions;

namespace Concertable.B2B.Seed.Infrastructure.Factories;

public static class FlatFeeDealFactory
{
    public static FlatFeeDealEntity Create(int id, decimal fee, PaymentMethod paymentMethod = PaymentMethod.Cash)
        => DealFactory.RequireValid(FlatFeeDealEntity.Create(fee, paymentMethod), id);
}

public static class VersusDealFactory
{
    public static VersusDealEntity Create(int id, decimal guarantee, decimal artistDoorPercent, PaymentMethod paymentMethod = PaymentMethod.Cash)
        => DealFactory.RequireValid(VersusDealEntity.Create(guarantee, artistDoorPercent, paymentMethod), id);
}

public static class DoorSplitDealFactory
{
    public static DoorSplitDealEntity Create(int id, decimal artistDoorPercent, PaymentMethod paymentMethod = PaymentMethod.Cash)
        => DealFactory.RequireValid(DoorSplitDealEntity.Create(artistDoorPercent, paymentMethod), id);
}

public static class VenueHireDealFactory
{
    public static VenueHireDealEntity Create(int id, decimal hireFee, PaymentMethod paymentMethod = PaymentMethod.Cash)
        => DealFactory.RequireValid(VenueHireDealEntity.Create(hireFee, paymentMethod), id);
}

internal static class DealFactory
{
    internal static TDeal RequireValid<TDeal>(Result<TDeal, ValidationErrors> result, int id)
        where TDeal : DealEntity =>
        result.Match(
            deal => deal.WithId(id),
            _ => throw new InvalidOperationException($"Seed deal {id} is invalid."));
}
