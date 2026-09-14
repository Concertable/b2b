using Dunet;
using Reunion.Errors;

namespace Concertable.B2B.Booking.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record ContractError : IError
{
    public ErrorDefinition Definition => this switch
    {
        ApplicationNotFound(var applicationId) =>
            ErrorDefinition.NotFound<ApplicationNotFound>(
                $"No contract was found for application {applicationId}."),
        BookingNotFound(var bookingId) =>
            ErrorDefinition.NotFound<BookingNotFound>(
                $"No contract was found for booking {bookingId}.")
    };

    [ErrorCode("contract.get_by_application.not_found")]
    public partial record ApplicationNotFound(int ApplicationId);

    [ErrorCode("contract.get_by_booking.not_found")]
    public partial record BookingNotFound(int BookingId);
}
