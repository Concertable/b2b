using Dunet;

namespace Concertable.B2B.Concert.Domain.Errors;

[Union(EnableImplicitConversions = false)]
public abstract partial record ConcertSummaryShareRevocationError : IError
{
    public ErrorDefinition Definition => this switch
    {
        GrantNotFound => ErrorDefinition.NotFound<GrantNotFound>(
            "No such grant on this concert."),
        NotAShare => ErrorDefinition.Invalid<NotAShare>(
            "A principal's own access and a member's assignment are not shares, and are not revoked here."),
        NotTheIssuer => ErrorDefinition.Forbidden<NotTheIssuer>(
            "Only the tenant that issued a share can revoke it.")
    };

    public partial record GrantNotFound;
    public partial record NotAShare;
    public partial record NotTheIssuer;
}
