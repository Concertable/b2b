using Dunet;

namespace Concertable.B2B.Concert.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record RevokeConcertSummaryShareError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NotPermitted => ErrorDefinition.Forbidden<NotPermitted>(
            "This call is not acting for a business that may revoke this share."),
        ConcertNotFound(var concertId) => ErrorDefinition.NotFound<ConcertNotFound>(
            $"Concert {concertId} was not found."),
        GrantNotFound => ErrorDefinition.NotFound<GrantNotFound>(
            "No such grant on this concert."),
        NotAShare => ErrorDefinition.Invalid<NotAShare>(
            "A principal's own access and a member's assignment are not shares, and are not revoked here."),
        NotTheIssuer => ErrorDefinition.Forbidden<NotTheIssuer>(
            "Only the business that issued a share can revoke it."),
        Superseded(var concertId) => ErrorDefinition.Conflict<Superseded>(
            $"Concert {concertId} changed while this revocation was in flight.")
    };

    [ErrorCode("concert.summary_share.revoke.not_permitted")]
    public partial record NotPermitted;

    [ErrorCode("concert.summary_share.revoke.not_found")]
    public partial record ConcertNotFound(int ConcertId);

    [ErrorCode("concert.summary_share.revoke.grant_not_found")]
    public partial record GrantNotFound;

    [ErrorCode("concert.summary_share.revoke.not_a_share")]
    public partial record NotAShare;

    [ErrorCode("concert.summary_share.revoke.not_the_issuer")]
    public partial record NotTheIssuer;

    [ErrorCode("concert.summary_share.revoke.superseded")]
    public partial record Superseded(int ConcertId);
}
