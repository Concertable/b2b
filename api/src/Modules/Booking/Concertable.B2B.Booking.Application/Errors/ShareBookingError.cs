using Concertable.B2B.Booking.Contracts.Enums;
using Dunet;
using Reunion.Errors;

namespace Concertable.B2B.Booking.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record ShareBookingError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NoActiveTenant => ErrorDefinition.Forbidden<NoActiveTenant>(
            "This call is not acting in a tenant."),
        BookingNotFound(var bookingId) => ErrorDefinition.NotFound<BookingNotFound>(
            $"Booking {bookingId} was not found."),
        ScopeNotShareable(var scope) => ErrorDefinition.Invalid<ScopeNotShareable>(
            $"A booking's {scope} is not disclosed by sharing it."),
        NotAPrincipal => ErrorDefinition.Forbidden<NotAPrincipal>(
            "Only a party to the booking can disclose it."),
        AlreadyShared => ErrorDefinition.Conflict<AlreadyShared>(
            "That audience already holds a live grant at this scope."),
        Superseded(var bookingId) => ErrorDefinition.Conflict<Superseded>(
            $"Booking {bookingId} changed while this share was in flight.")
    };

    [ErrorCode("booking.share.no_active_tenant")]
    public partial record NoActiveTenant;

    [ErrorCode("booking.share.not_found")]
    public partial record BookingNotFound(int BookingId);

    [ErrorCode("booking.share.scope_not_shareable")]
    public partial record ScopeNotShareable(BookingAccessScope Scope);

    [ErrorCode("booking.share.not_a_principal")]
    public partial record NotAPrincipal;

    [ErrorCode("booking.share.already_shared")]
    public partial record AlreadyShared;

    [ErrorCode("booking.share.superseded")]
    public partial record Superseded(int BookingId);
}
