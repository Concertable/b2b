using Concertable.B2B.Concert.Contracts.Enums;
using Dunet;

namespace Concertable.B2B.Concert.Domain.Errors;

[Union(EnableImplicitConversions = false)]
public abstract partial record ConcertShareError : IError
{
    public ErrorDefinition Definition => this switch
    {
        ScopeNotShareable(var scope) => ErrorDefinition.Invalid<ScopeNotShareable>(
            $"A concert's {scope} is not disclosed by sharing it."),
        NotAPrincipal => ErrorDefinition.Forbidden<NotAPrincipal>(
            "Only a party to the concert can disclose it."),
        AlreadyShared => ErrorDefinition.Conflict<AlreadyShared>(
            "That audience already holds a live grant at this scope.")
    };

    public partial record ScopeNotShareable(ConcertAccessScope Scope);
    public partial record NotAPrincipal;
    public partial record AlreadyShared;
}
