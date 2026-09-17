namespace Concertable.B2B.Application.Domain.Lifecycle;

/// <summary>Which lifecycle states commit the tenant to money still owed in this stage. Deny-by-default: an
/// unlisted state blocks erasure until it is deliberately classified here.</summary>
public static class ApplicationObligation
{
    public static IReadOnlySet<ApplicationState> SettledStates { get; } = new HashSet<ApplicationState>
    {
        ApplicationState.Applied,
        ApplicationState.Accepted,
        ApplicationState.Rejected,
        ApplicationState.Withdrawn,
        ApplicationState.Cancelled,
    };

    public static bool IsLive(ApplicationState state) => !SettledStates.Contains(state);
}
