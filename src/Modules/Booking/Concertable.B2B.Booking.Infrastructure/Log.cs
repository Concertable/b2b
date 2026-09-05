using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Booking.Infrastructure;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Accepting application {ApplicationId} (booking {BookingId}): capturing the authorization held under {AuthorizationReference} for {Amount} {Currency} from {PayerId} on behalf of {PayeeId}")]
    internal static partial void AcceptingFlatFeeApplication(
        this ILogger logger,
        int applicationId,
        int bookingId,
        string authorizationReference,
        decimal amount,
        string currency,
        Guid payerId,
        Guid payeeId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Accepting application {ApplicationId} (booking {BookingId}): charging {Amount} GBP from {PayerId} on behalf of {PayeeId}")]
    internal static partial void AcceptingVenueHireApplication(
        this ILogger logger,
        int applicationId,
        int bookingId,
        decimal amount,
        Guid payerId,
        Guid payeeId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Contract {ContractId}: stored {Party} drawn signature could not be decoded; rendering without the drawn image")]
    internal static partial void DrawnSignatureDecodeFailed(
        this ILogger logger,
        int contractId,
        string party);
}
