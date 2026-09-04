using System.Net;
using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Contracts.Enums;
using Concertable.B2B.Infrastructure.Payments;
using Concertable.Contracts.Enums;

namespace Concertable.B2B.Booking.UnitTests;

internal static class AcceptedApplications
{
    private const int ApplicationId = 42;
    private const int OpportunityId = 43;

    public static AcceptedApplication FlatFee() =>
        new(Snapshot(new FlatFeeTerms(100m), PaymentCommitmentTokens.EscrowHold));

    public static AcceptedApplication DoorSplit() =>
        new(Snapshot(new DoorSplitTerms(70m), PaymentCommitmentTokens.MethodVerification));

    public static AcceptedApplication VenueHire() =>
        new(Snapshot(new VenueHireTerms(250m), PaymentCommitmentTokens.MethodSetup));

    private static ApplicationAcceptanceSnapshot Snapshot(DealTerms terms, string operationType) => new(
        Guid.Parse("11111111-1111-1111-1111-111111111111"),
        new ApplicationSnapshot(
            ApplicationId,
            new ArtistSnapshot(44, Guid.Parse("33333333-3333-3333-3333-333333333333"), "Artist"),
            new OpportunitySnapshot(
                OpportunityId,
                new VenueSnapshot(45, Guid.Parse("22222222-2222-2222-2222-222222222222"), "Venue"),
                new DateTime(2030, 1, 1, 19, 0, 0, DateTimeKind.Utc),
                new DateTime(2030, 1, 1, 22, 0, 0, DateTimeKind.Utc),
                [Genre.Rock])),
        new ContractSnapshot(
            PaymentMethod.Transfer,
            "Terms",
            "1",
            "2026-09",
            new PaymentCommitment(
                operationType,
                PaymentCommitmentCorrelation.ForApplication(ApplicationId)),
            Signature("Artist"),
            Signature("Venue"),
            terms));

    private static ContractSignature Signature(string name) => new(
        Guid.NewGuid(),
        new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc),
        IPAddress.Loopback,
        "tests",
        name,
        null);
}
