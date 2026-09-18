using Concertable.B2B.Application.Contracts.Enums;
using Dunet;
using Reunion.Errors;

namespace Concertable.B2B.Application.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record ShareApplicationError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NoActiveTenant => ErrorDefinition.Forbidden<NoActiveTenant>(
            "This call is not acting in a tenant."),
        ApplicationNotFound(var applicationId) => ErrorDefinition.NotFound<ApplicationNotFound>(
            $"Application {applicationId} was not found."),
        ScopeNotShareable(var scope) => ErrorDefinition.Invalid<ScopeNotShareable>(
            $"An application's {scope} is not disclosed by sharing it."),
        NotAPrincipal => ErrorDefinition.Forbidden<NotAPrincipal>(
            "Only a party to the application can disclose it."),
        AlreadyShared => ErrorDefinition.Conflict<AlreadyShared>(
            "That audience already holds a live grant at this scope."),
        Superseded(var applicationId) => ErrorDefinition.Conflict<Superseded>(
            $"Application {applicationId} changed while this share was in flight.")
    };

    [ErrorCode("application.share.no_active_tenant")]
    public partial record NoActiveTenant;

    [ErrorCode("application.share.not_found")]
    public partial record ApplicationNotFound(int ApplicationId);

    [ErrorCode("application.share.scope_not_shareable")]
    public partial record ScopeNotShareable(ApplicationAccessScope Scope);

    [ErrorCode("application.share.not_a_principal")]
    public partial record NotAPrincipal;

    [ErrorCode("application.share.already_shared")]
    public partial record AlreadyShared;

    [ErrorCode("application.share.superseded")]
    public partial record Superseded(int ApplicationId);
}
