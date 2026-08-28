using Concertable.B2B.Application.Domain.Lifecycle;
using Concertable.Kernel;
using Dunet;

namespace Concertable.B2B.Application.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record RejectApplicationError : IError
{
    public ErrorDefinition Definition => this switch
    {
        ApplicationNotFound(var applicationId) =>
            ErrorDefinition.NotFound<ApplicationNotFound>($"Application {applicationId} was not found."),
        InvalidTransition(var error) =>
            ErrorDefinition.Conflict<InvalidTransition>($"Cannot reject an application from {error.Current}."),
        Superseded(var applicationId) => ErrorDefinition.Conflict<Superseded>(
            $"Application {applicationId} changed while this reject was in flight.")
    };

    [ErrorCode("application.reject.not_found")]
    public partial record ApplicationNotFound(int ApplicationId);

    [ErrorCode("application.reject.invalid_state")]
    public partial record InvalidTransition(TransitionError<State, Trigger> Error);

    [ErrorCode("application.reject.superseded")]
    public partial record Superseded(int ApplicationId);
}
