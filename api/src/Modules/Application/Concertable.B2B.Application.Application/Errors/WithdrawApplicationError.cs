using Concertable.B2B.Application.Domain.Lifecycle;
using Concertable.Kernel;
using Dunet;

namespace Concertable.B2B.Application.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record WithdrawApplicationError : IError
{
    public ErrorDefinition Definition => this switch
    {
        ApplicationNotFound(var applicationId) =>
            ErrorDefinition.NotFound<ApplicationNotFound>($"Application {applicationId} was not found."),
        InvalidTransition(var error) =>
            ErrorDefinition.Conflict<InvalidTransition>($"Cannot withdraw an application from {error.Current}."),
        Superseded(var applicationId) => ErrorDefinition.Conflict<Superseded>(
            $"Application {applicationId} changed while this withdraw was in flight.")
    };

    [ErrorCode("application.withdraw.not_found")]
    public partial record ApplicationNotFound(int ApplicationId);

    [ErrorCode("application.withdraw.invalid_state")]
    public partial record InvalidTransition(TransitionError<ApplicationState, ApplicationTrigger> Error);

    [ErrorCode("application.withdraw.superseded")]
    public partial record Superseded(int ApplicationId);
}
