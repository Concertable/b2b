using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Domain.Factories;
using Concertable.B2B.Deal.Contracts;

namespace Concertable.B2B.Booking.Infrastructure.Strategies;

internal abstract class ContractFactory<TTerms> : IContractFactory<TTerms>
    where TTerms : DealTerms
{
    public ContractEntity Create(
        int bookingId,
        ApplicationAcceptanceSnapshot snapshot,
        DateTime createdAtUtc) =>
        Create(bookingId, snapshot, (TTerms)snapshot.Contract.Terms, createdAtUtc);

    public abstract ContractEntity Create(
        int bookingId,
        ApplicationAcceptanceSnapshot snapshot,
        TTerms terms,
        DateTime createdAtUtc);
}

internal sealed class FlatFeeContractFactory : ContractFactory<FlatFeeTerms>
{
    public override ContractEntity Create(
        int bookingId, ApplicationAcceptanceSnapshot snapshot, FlatFeeTerms terms, DateTime createdAtUtc) =>
        FlatFeeContract.Create(bookingId, snapshot, terms, createdAtUtc);
}

internal sealed class VenueHireContractFactory : ContractFactory<VenueHireTerms>
{
    public override ContractEntity Create(
        int bookingId,
        ApplicationAcceptanceSnapshot snapshot,
        VenueHireTerms terms,
        DateTime createdAtUtc) =>
        VenueHireContract.Create(bookingId, snapshot, terms, createdAtUtc);
}

internal sealed class DoorSplitContractFactory : ContractFactory<DoorSplitTerms>
{
    public override ContractEntity Create(
        int bookingId,
        ApplicationAcceptanceSnapshot snapshot,
        DoorSplitTerms terms,
        DateTime createdAtUtc) =>
        DoorSplitContract.Create(bookingId, snapshot, terms, createdAtUtc);
}

internal sealed class VersusContractFactory : ContractFactory<VersusTerms>
{
    public override ContractEntity Create(
        int bookingId,
        ApplicationAcceptanceSnapshot snapshot,
        VersusTerms terms,
        DateTime createdAtUtc) =>
        VersusContract.Create(bookingId, snapshot, terms, createdAtUtc);
}
