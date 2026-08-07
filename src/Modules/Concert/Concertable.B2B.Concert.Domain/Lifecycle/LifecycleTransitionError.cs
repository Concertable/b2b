using Dunet;

namespace Concertable.B2B.Concert.Domain.Lifecycle;

[Union(EnableImplicitConversions = false)]
internal abstract partial record LifecycleTransitionError : IError
{
    public ErrorDefinition Definition => this switch
    {
        ApplicationNotFound(var applicationId) =>
            ErrorDefinition.NotFound<ApplicationNotFound>(
                $"Application {applicationId} was not found."),
        InvalidTransition(var current, var trigger) =>
            ErrorDefinition.Conflict<InvalidTransition>(
                $"Cannot {trigger} from {current}.")
    };

    [ErrorCode("concert.lifecycle.application_not_found")]
    public partial record ApplicationNotFound(int ApplicationId);

    [ErrorCode("concert.lifecycle.invalid_transition")]
    public partial record InvalidTransition(LifecycleState Current, Trigger Trigger);
}
