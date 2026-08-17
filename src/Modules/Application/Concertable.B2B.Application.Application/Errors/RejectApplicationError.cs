using Concertable.B2B.Application.Domain.State;
using Dunet;

namespace Concertable.B2B.Application.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record RejectApplicationError : IError
{
    public ErrorDefinition Definition => this switch
    {
        ApplicationNotFound(var applicationId) =>
            ErrorDefinition.NotFound<ApplicationNotFound>($"Application {applicationId} was not found."),
        InvalidState(var state) =>
            ErrorDefinition.Conflict<InvalidState>($"Cannot reject an application from {state}.")
    };

    [ErrorCode("application.reject.not_found")]
    public partial record ApplicationNotFound(int ApplicationId);

    [ErrorCode("application.reject.invalid_state")]
    public partial record InvalidState(ApplicationState State);
}
