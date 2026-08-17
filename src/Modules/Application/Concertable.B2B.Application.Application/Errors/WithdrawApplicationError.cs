using Concertable.B2B.Application.Domain.State;
using Dunet;

namespace Concertable.B2B.Application.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record WithdrawApplicationError : IError
{
    public ErrorDefinition Definition => this switch
    {
        ApplicationNotFound(var applicationId) =>
            ErrorDefinition.NotFound<ApplicationNotFound>($"Application {applicationId} was not found."),
        InvalidState(var state) =>
            ErrorDefinition.Conflict<InvalidState>($"Cannot withdraw an application from {state}.")
    };

    [ErrorCode("application.withdraw.not_found")]
    public partial record ApplicationNotFound(int ApplicationId);

    [ErrorCode("application.withdraw.invalid_state")]
    public partial record InvalidState(ApplicationState State);
}
