using Reunion.Errors;
using Dunet;

namespace Concertable.B2B.Booking.Domain.Errors;

[Union(EnableImplicitConversions = false)]
public abstract partial record BookingShareRevocationError : IError
{
    public ErrorDefinition Definition => this switch
    {
        GrantNotFound => ErrorDefinition.NotFound<GrantNotFound>(
            "No such grant on this booking."),
        NotAShare => ErrorDefinition.Invalid<NotAShare>(
            "A party's own access to its booking is not a share, and is not revoked here."),
        NotTheIssuer => ErrorDefinition.Forbidden<NotTheIssuer>(
            "Only the tenant that issued a share can revoke it.")
    };

    public partial record GrantNotFound;
    public partial record NotAShare;
    public partial record NotTheIssuer;
}
