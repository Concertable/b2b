using Concertable.B2B.Contract.Contracts;
using Concertable.B2B.Contract.Domain.Entities;
using static Concertable.Seed.Identity.Extensions.EntityReflectionExtensions;

namespace Concertable.B2B.Seed.Infrastructure.Factories;

public static class FlatFeeContractFactory
{
    public static FlatFeeContractEntity Create(int id, decimal fee, PaymentMethod paymentMethod = PaymentMethod.Cash)
        => FlatFeeContractEntity.Create(fee, paymentMethod).With(nameof(ContractEntity.Id), id);
}

public static class VersusContractFactory
{
    public static VersusContractEntity Create(int id, decimal guarantee, decimal artistDoorPercent, PaymentMethod paymentMethod = PaymentMethod.Cash)
        => VersusContractEntity.Create(guarantee, artistDoorPercent, paymentMethod).With(nameof(ContractEntity.Id), id);
}

public static class DoorSplitContractFactory
{
    public static DoorSplitContractEntity Create(int id, decimal artistDoorPercent, PaymentMethod paymentMethod = PaymentMethod.Cash)
        => DoorSplitContractEntity.Create(artistDoorPercent, paymentMethod).With(nameof(ContractEntity.Id), id);
}

public static class VenueHireContractFactory
{
    public static VenueHireContractEntity Create(int id, decimal hireFee, PaymentMethod paymentMethod = PaymentMethod.Cash)
        => VenueHireContractEntity.Create(hireFee, paymentMethod).With(nameof(ContractEntity.Id), id);
}
