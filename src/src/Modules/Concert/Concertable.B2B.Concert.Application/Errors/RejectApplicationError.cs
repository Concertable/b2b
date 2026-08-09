using Concertable.B2B.Concert.Domain.Lifecycle;
using Dunet;

namespace Concertable.B2B.Concert.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record RejectApplicationError : IError
{
    public ErrorDefinition Definition => this switch
    {
        ApplicationNotFound(var applicationId) =>
            ErrorDefinition.For<RejectApplicationError>().NotFound<ApplicationNotFound>(
                $"Application {applicationId} was not found."),
        InvalidTransition(var current, var trigger) =>
            ErrorDefinition.For<RejectApplicationError>().Conflict<InvalidTransition>(
                $"Cannot {trigger} from {current}.")
    };

    [ErrorCode("application.reject.not_found")]
    public partial record ApplicationNotFound(int ApplicationId);

    [ErrorCode("application.reject.invalid_transition")]
    public partial record InvalidTransition(LifecycleState Current, Trigger Trigger);
}
