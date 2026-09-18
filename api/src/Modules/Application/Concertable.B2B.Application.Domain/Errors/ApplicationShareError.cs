using Reunion.Errors;
using Concertable.B2B.Application.Contracts.Enums;
using Dunet;

namespace Concertable.B2B.Application.Domain.Errors;

[Union(EnableImplicitConversions = false)]
public abstract partial record ApplicationShareError : IError
{
    public ErrorDefinition Definition => this switch
    {
        ScopeNotShareable(var scope) => ErrorDefinition.Invalid<ScopeNotShareable>(
            $"A application's {scope} is not disclosed by sharing it."),
        NotAPrincipal => ErrorDefinition.Forbidden<NotAPrincipal>(
            "Only a party to the application can disclose it."),
        AlreadyShared => ErrorDefinition.Conflict<AlreadyShared>(
            "That audience already holds a live grant at this scope.")
    };

    public partial record ScopeNotShareable(ApplicationAccessScope Scope);
    public partial record NotAPrincipal;
    public partial record AlreadyShared;
}
