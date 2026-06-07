using Concertable.B2B.Contract.Contracts;
using Concertable.B2B.Contract.Domain.Entities;
using static Concertable.Seed.Identity.Extensions.EntityReflectionExtensions;

namespace Concertable.B2B.Seed.Infrastructure.Factories;

public static class FlatFeeContractFactory
{
    public static FlatFeeContractEntity Create(int id, decimal fee, PaymentMethod paymentMethod = PaymentMethod.Cash)
        => FlatFeeContractEntity.Create(fee, paymentMethod).WithId(id);
}

public static class VersusContractFactory
{
    public static VersusContractEntity Create(int id, decimal guarantee, decimal artistDoorPercent, PaymentMethod paymentMethod = PaymentMethod.Cash)
        => VersusContractEntity.Create(guarantee, artistDoorPercent, paymentMethod).WithId(id);
}

public static class DoorSplitContractFactory
{
    public static DoorSplitContractEntity Create(int id, decimal artistDoorPercent, PaymentMethod paymentMethod = PaymentMethod.Cash)
        => DoorSplitContractEntity.Create(artistDoorPercent, paymentMethod).WithId(id);
}

public static class VenueHireContractFactory
{
    public static VenueHireContractEntity Create(int id, decimal hireFee, PaymentMethod paymentMethod = PaymentMethod.Cash)
        => VenueHireContractEntity.Create(hireFee, paymentMethod).WithId(id);
}
