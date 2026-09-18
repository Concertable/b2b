using Dunet;

namespace Concertable.B2B.Concert.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record RevokeConcertShareError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NoActiveTenant => ErrorDefinition.Forbidden<NoActiveTenant>(
            "This call is not acting in a tenant."),
        ConcertNotFound(var concertId) => ErrorDefinition.NotFound<ConcertNotFound>(
            $"Concert {concertId} was not found."),
        GrantNotFound => ErrorDefinition.NotFound<GrantNotFound>(
            "No such grant on this concert."),
        NotAShare => ErrorDefinition.Invalid<NotAShare>(
            "A party's own access to its concert is not a share, and is not revoked here."),
        NotTheIssuer => ErrorDefinition.Forbidden<NotTheIssuer>(
            "Only the tenant that issued a share can revoke it."),
        Superseded(var concertId) => ErrorDefinition.Conflict<Superseded>(
            $"Concert {concertId} changed while this revocation was in flight.")
    };

    [ErrorCode("concert.share.revoke.no_active_tenant")]
    public partial record NoActiveTenant;

    [ErrorCode("concert.share.revoke.not_found")]
    public partial record ConcertNotFound(int ConcertId);

    [ErrorCode("concert.share.revoke.grant_not_found")]
    public partial record GrantNotFound;

    [ErrorCode("concert.share.revoke.not_a_share")]
    public partial record NotAShare;

    [ErrorCode("concert.share.revoke.not_the_issuer")]
    public partial record NotTheIssuer;

    [ErrorCode("concert.share.revoke.superseded")]
    public partial record Superseded(int ConcertId);
}
