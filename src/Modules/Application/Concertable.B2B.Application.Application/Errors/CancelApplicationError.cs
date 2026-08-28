using Concertable.B2B.Application.Domain.Lifecycle;
using Concertable.Kernel;
using Dunet;

namespace Concertable.B2B.Application.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record CancelApplicationError : IError
{
    public ErrorDefinition Definition => this switch
    {
        ApplicationNotFound(var applicationId) =>
            ErrorDefinition.NotFound<ApplicationNotFound>($"Application {applicationId} was not found."),
        InvalidTransition(var error) =>
            ErrorDefinition.Conflict<InvalidTransition>($"Cannot cancel an application from {error.Current}.")
    };

    [ErrorCode("application.cancel.not_found")]
    public partial record ApplicationNotFound(int ApplicationId);

    [ErrorCode("application.cancel.invalid_state")]
    public partial record InvalidTransition(TransitionError<State, Trigger> Error);
}
