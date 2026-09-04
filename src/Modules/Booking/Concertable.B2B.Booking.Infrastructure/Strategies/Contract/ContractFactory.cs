using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Domain.Factories;
using Concertable.B2B.Booking.Domain.Financial;
using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Infrastructure.Payments;

namespace Concertable.B2B.Booking.Infrastructure.Strategies;

internal abstract class ContractFactory<TTerms> : IContractFactory<TTerms>
    where TTerms : DealTerms
{
    public ContractEntity Create(
        int bookingId,
        ApplicationAcceptanceSnapshot snapshot,
        DateTime createdAtUtc)
    {
        var contract = Create(bookingId, snapshot, (TTerms)snapshot.Contract.Terms, createdAtUtc);
        // The persisted pair is the durable key Payment indexed, so it is frozen rather than derived on
        // read. This is the one place the minted token and the arm's declared operation are both known.
        var expected = contract.ExpectedFinancialOperation switch
        {
            FinancialOperation.CaptureEscrow => PaymentCommitmentTokens.EscrowHold,
            FinancialOperation.DepositEscrow => PaymentCommitmentTokens.MethodSetup,
            FinancialOperation.VerifyPayment => PaymentCommitmentTokens.MethodVerification,
            var operation => throw new ArgumentOutOfRangeException(nameof(snapshot), operation, null)
        };
        if (contract.Commitment.OperationType != expected)
            throw new InvalidOperationException(
                $"Commitment {contract.Commitment.OperationType} does not name the "
                + $"{contract.ExpectedFinancialOperation} operation this contract expects.");

        return contract;
    }

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
