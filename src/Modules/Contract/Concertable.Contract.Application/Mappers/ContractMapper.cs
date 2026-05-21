using Concertable.Contract.Application.Interfaces;

namespace Concertable.Contract.Application.Mappers;

internal sealed class ContractMapper : IContractMapper
{
    public IContract ToContract(ContractEntity entity) => entity switch
    {
        FlatFeeContractEntity e => new FlatFeeContract
        {
            Id = e.Id,
            PaymentMethod = e.PaymentMethod,
            Fee = e.Fee
        },
        DoorSplitContractEntity e => new DoorSplitContract
        {
            Id = e.Id,
            PaymentMethod = e.PaymentMethod,
            ArtistDoorPercent = e.ArtistDoorPercent
        },
        VersusContractEntity e => new VersusContract
        {
            Id = e.Id,
            PaymentMethod = e.PaymentMethod,
            Guarantee = e.Guarantee,
            ArtistDoorPercent = e.ArtistDoorPercent
        },
        VenueHireContractEntity e => new VenueHireContract
        {
            Id = e.Id,
            PaymentMethod = e.PaymentMethod,
            HireFee = e.HireFee
        },
        _ => throw new InvalidOperationException($"Unknown contract type {entity.GetType().Name}")
    };

    public ContractEntity ToEntity(IContract contract) => contract switch
    {
        FlatFeeContract c => FlatFeeContractEntity.Create(c.Fee, c.PaymentMethod),
        DoorSplitContract c => DoorSplitContractEntity.Create(c.ArtistDoorPercent, c.PaymentMethod),
        VersusContract c => VersusContractEntity.Create(c.Guarantee, c.ArtistDoorPercent, c.PaymentMethod),
        VenueHireContract c => VenueHireContractEntity.Create(c.HireFee, c.PaymentMethod),
        _ => throw new InvalidOperationException($"Unknown contract type {contract.GetType().Name}")
    };
}
