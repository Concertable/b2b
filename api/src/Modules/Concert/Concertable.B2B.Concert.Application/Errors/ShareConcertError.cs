using Concertable.B2B.Concert.Contracts.Enums;
using Dunet;

namespace Concertable.B2B.Concert.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record ShareConcertError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NoActiveTenant => ErrorDefinition.Forbidden<NoActiveTenant>(
            "This call is not acting in a tenant."),
        ConcertNotFound(var concertId) => ErrorDefinition.NotFound<ConcertNotFound>(
            $"Concert {concertId} was not found."),
        ScopeNotShareable(var scope) => ErrorDefinition.Invalid<ScopeNotShareable>(
            $"A concert's {scope} is not disclosed by sharing it."),
        NotAPrincipal => ErrorDefinition.Forbidden<NotAPrincipal>(
            "Only a party to the concert can disclose it."),
        AlreadyShared => ErrorDefinition.Conflict<AlreadyShared>(
            "That audience already holds a live grant at this scope."),
        Superseded(var concertId) => ErrorDefinition.Conflict<Superseded>(
            $"Concert {concertId} changed while this share was in flight.")
    };

    [ErrorCode("concert.share.no_active_tenant")]
    public partial record NoActiveTenant;

    [ErrorCode("concert.share.not_found")]
    public partial record ConcertNotFound(int ConcertId);

    [ErrorCode("concert.share.scope_not_shareable")]
    public partial record ScopeNotShareable(ConcertAccessScope Scope);

    [ErrorCode("concert.share.not_a_principal")]
    public partial record NotAPrincipal;

    [ErrorCode("concert.share.already_shared")]
    public partial record AlreadyShared;

    [ErrorCode("concert.share.superseded")]
    public partial record Superseded(int ConcertId);
}
