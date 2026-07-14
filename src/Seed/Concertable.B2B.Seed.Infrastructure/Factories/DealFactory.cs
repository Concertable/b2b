using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Domain.Entities;
using static Concertable.Seed.Identity.Extensions.EntityReflectionExtensions;

namespace Concertable.B2B.Seed.Infrastructure.Factories;

public static class FlatFeeDealFactory
{
    public static FlatFeeDealEntity Create(int id, decimal fee, PaymentMethod paymentMethod = PaymentMethod.Cash)
        => FlatFeeDealEntity.Create(fee, paymentMethod).WithId(id);
}

public static class VersusDealFactory
{
    public static VersusDealEntity Create(int id, decimal guarantee, decimal artistDoorPercent, PaymentMethod paymentMethod = PaymentMethod.Cash)
        => VersusDealEntity.Create(guarantee, artistDoorPercent, paymentMethod).WithId(id);
}

public static class DoorSplitDealFactory
{
    public static DoorSplitDealEntity Create(int id, decimal artistDoorPercent, PaymentMethod paymentMethod = PaymentMethod.Cash)
        => DoorSplitDealEntity.Create(artistDoorPercent, paymentMethod).WithId(id);
}

public static class VenueHireDealFactory
{
    public static VenueHireDealEntity Create(int id, decimal hireFee, PaymentMethod paymentMethod = PaymentMethod.Cash)
        => VenueHireDealEntity.Create(hireFee, paymentMethod).WithId(id);
}
