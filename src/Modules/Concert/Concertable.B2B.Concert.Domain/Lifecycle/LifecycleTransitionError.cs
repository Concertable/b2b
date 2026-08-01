namespace Concertable.B2B.Concert.Domain.Lifecycle;

internal sealed record LifecycleTransitionError : IError
{
    private LifecycleTransitionError(ErrorDefinition definition)
    {
        Definition = definition;
    }

    public ErrorDefinition Definition { get; }

    internal static LifecycleTransitionError ApplicationNotFound(int applicationId) =>
        new(ErrorDefinition.NotFound(
            "concert.lifecycle.application_not_found",
            $"Application {applicationId} was not found."));

    internal static LifecycleTransitionError Invalid(LifecycleState current, Trigger trigger) =>
        new(ErrorDefinition.Conflict(
            "concert.lifecycle.invalid_transition",
            $"Cannot {trigger} from {current}."));
}
