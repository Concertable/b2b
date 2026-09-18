using Dunet;
using Reunion.Errors;

namespace Concertable.B2B.Booking.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record RevokeBookingShareError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NoActiveTenant => ErrorDefinition.Forbidden<NoActiveTenant>(
            "This call is not acting in a tenant."),
        BookingNotFound(var bookingId) => ErrorDefinition.NotFound<BookingNotFound>(
            $"Booking {bookingId} was not found."),
        GrantNotFound => ErrorDefinition.NotFound<GrantNotFound>(
            "No such grant on this booking."),
        NotAShare => ErrorDefinition.Invalid<NotAShare>(
            "A party's own access to its booking is not a share, and is not revoked here."),
        NotTheIssuer => ErrorDefinition.Forbidden<NotTheIssuer>(
            "Only the tenant that issued a share can revoke it."),
        Superseded(var bookingId) => ErrorDefinition.Conflict<Superseded>(
            $"Booking {bookingId} changed while this revocation was in flight.")
    };

    [ErrorCode("booking.share.revoke.no_active_tenant")]
    public partial record NoActiveTenant;

    [ErrorCode("booking.share.revoke.not_found")]
    public partial record BookingNotFound(int BookingId);

    [ErrorCode("booking.share.revoke.grant_not_found")]
    public partial record GrantNotFound;

    [ErrorCode("booking.share.revoke.not_a_share")]
    public partial record NotAShare;

    [ErrorCode("booking.share.revoke.not_the_issuer")]
    public partial record NotTheIssuer;

    [ErrorCode("booking.share.revoke.superseded")]
    public partial record Superseded(int BookingId);
}
