namespace Concertable.B2B.Application.Domain.Lifecycle;

internal sealed class ApplicationStateMachine() : Concertable.Kernel.StateMachine<ApplicationState, ApplicationTrigger>(
[
    (ApplicationState.Applied, ApplicationTrigger.Accept, ApplicationState.Accepted),
    (ApplicationState.Applied, ApplicationTrigger.Reject, ApplicationState.Rejected),
    (ApplicationState.Applied, ApplicationTrigger.Withdraw, ApplicationState.Withdrawn),
    (ApplicationState.Applied, ApplicationTrigger.Cancel, ApplicationState.Cancelled)
]);
