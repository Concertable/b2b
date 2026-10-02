namespace Concertable.B2B.Application.Domain.Lifecycle;

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
