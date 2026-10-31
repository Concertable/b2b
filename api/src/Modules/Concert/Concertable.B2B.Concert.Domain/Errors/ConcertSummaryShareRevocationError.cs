using Dunet;

namespace Concertable.B2B.Concert.Domain.Errors;

[Union(EnableImplicitConversions = false)]
public abstract partial record ConcertSummaryShareRevocationError : IError
{
    public ErrorDefinition Definition => this switch
    {
        GrantNotFound => ErrorDefinition.NotFound<GrantNotFound>(
            "No such grant on this concert."),
        NotTheIssuer => ErrorDefinition.Forbidden<NotTheIssuer>(
            "Only the tenant that issued a share can revoke it.")
    };

    public partial record GrantNotFound;
    public partial record NotTheIssuer;
}
