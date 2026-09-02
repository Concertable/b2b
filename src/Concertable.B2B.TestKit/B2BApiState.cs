using System.Text.Json;

namespace Concertable.B2B.TestKit;

public enum B2BApplicationStatus
{
    Pending,
    Rejected,
    Withdrawn,
    Accepted,
    AwaitingPayment,
    Confirmed,
    Cancelled,
}

public enum B2BConcertLifecycleState
{
    Draft,
    Posted,
    CancellationPending,
    CancellationFailed,
    AwaitingSettlement,
    SettlementFailed,
    Complete,
    Cancelled
}

public static class B2BTestFunctions
{
    public const string ConcertFinished = "ConcertFinishedFunction";
}

public sealed record B2BApplicationState(B2BApplicationStatus Status);

public sealed record B2BCheckoutState(B2BCheckoutSession Session);

public sealed record B2BCheckoutSession(string ClientSecret);

public sealed record B2BConcertState
{
    public int Id { get; init; }
    public required B2BConcertActions Actions { get; init; }
}

public sealed record B2BConcertActions(JsonElement? Cancel);
