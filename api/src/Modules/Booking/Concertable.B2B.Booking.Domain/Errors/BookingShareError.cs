using Reunion.Errors;
using Concertable.B2B.Booking.Contracts.Enums;
using Dunet;

namespace Concertable.B2B.Booking.Domain.Errors;

[Union(EnableImplicitConversions = false)]
public abstract partial record BookingShareError : IError
{
    public ErrorDefinition Definition => this switch
    {
        ScopeNotShareable(var scope) => ErrorDefinition.Invalid<ScopeNotShareable>(
            $"A booking's {scope} is not disclosed by sharing it."),
        NotAPrincipal => ErrorDefinition.Forbidden<NotAPrincipal>(
            "Only a party to the booking can disclose it."),
        AlreadyShared => ErrorDefinition.Conflict<AlreadyShared>(
            "That audience already holds a live grant at this scope.")
    };

    public partial record ScopeNotShareable(BookingAccessScope Scope);
    public partial record NotAPrincipal;
    public partial record AlreadyShared;
}
