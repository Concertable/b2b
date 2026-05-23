using FluentResults;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Duplicate inbox message {MessageId}; skipping")]
    internal static partial void DuplicateInboxMessage(this ILogger logger, Guid messageId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Escrow webhook received: payment intent {TransactionId} for booking {BookingId}")]
    internal static partial void EscrowWebhookReceived(this ILogger logger, string transactionId, int bookingId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Verify webhook received: payment intent {TransactionId} for application {ApplicationId}")]
    internal static partial void VerifyWebhookReceived(this ILogger logger, string transactionId, int applicationId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Verify payment failed for application {ApplicationId}: [{FailureCode}] {FailureMessage}")]
    internal static partial void VerifyPaymentFailed(this ILogger logger, int applicationId, string? failureCode, string? failureMessage);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Concert {ConcertId} not found for ticket sale")]
    internal static partial void ConcertNotFoundForTicketSale(this ILogger logger, int concertId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Settlement webhook received: payment intent {TransactionId} for booking {BookingId}")]
    internal static partial void SettlementWebhookReceived(this ILogger logger, string transactionId, int bookingId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Payment failed for booking {BookingId}: [{FailureCode}] {FailureMessage}")]
    internal static partial void BookingPaymentFailed(this ILogger logger, int bookingId, string? failureCode, string? failureMessage);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Calculated versus artist share for concert {ConcertId}: {Guarantee} guarantee + ({Revenue} revenue at {Percent}%) = {Share}")]
    internal static partial void VersusArtistShareCalculated(this ILogger logger, int concertId, decimal guarantee, decimal revenue, decimal percent, decimal share);

    [LoggerMessage(Level = LogLevel.Information, Message = "Settling concert {ConcertId} (booking {BookingId}): paying {Amount} GBP from {PayerId} to {PayeeId}")]
    internal static partial void SettlingConcert(this ILogger logger, int concertId, int bookingId, decimal amount, Guid payerId, Guid payeeId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Accepting application {ApplicationId} (booking {BookingId}): charging {Amount} GBP from {PayerId} on behalf of {PayeeId}")]
    internal static partial void AcceptingVenueHireApplication(this ILogger logger, int applicationId, int bookingId, decimal amount, Guid payerId, Guid payeeId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Accepting application {ApplicationId} (booking {BookingId}): binding pre-authorised PaymentIntent {PaymentIntentId} for {Amount} {Currency} from {PayerId} on behalf of {PayeeId}")]
    internal static partial void AcceptingFlatFeeApplication(this ILogger logger, int applicationId, int bookingId, string paymentIntentId, decimal amount, string currency, Guid payerId, Guid payeeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Calculated door-split artist share for concert {ConcertId}: {Revenue} revenue at {Percent}% = {Share}")]
    internal static partial void DoorSplitArtistShareCalculated(this ILogger logger, int concertId, decimal revenue, decimal percent, decimal share);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to finish concert {ConcertId}")]
    internal static partial void FailedToFinishConcert(this ILogger logger, int concertId, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to finish concert {ConcertId}: {Errors}")]
    internal static partial void ConcertCompletionFailed(this ILogger logger, int concertId, IReadOnlyList<IError> errors);

    [LoggerMessage(Level = LogLevel.Information, Message = "Creating concert draft for booking {BookingId}")]
    internal static partial void CreatingConcertDraft(this ILogger logger, int bookingId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Concert draft creation failed for booking {BookingId}: artist {ArtistId} has no matching genres for opportunity {OpportunityId}")]
    internal static partial void ConcertDraftCreationFailed(this ILogger logger, int bookingId, int artistId, int opportunityId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Concert draft {ConcertId} created for booking {BookingId} (artist {ArtistId}, venue {VenueId}); notifying users")]
    internal static partial void ConcertDraftCreated(this ILogger logger, int concertId, int bookingId, int artistId, int venueId);

    [LoggerMessage(Level = LogLevel.Information, Message = "ConcertCompletionRunner: found {Count} ended confirmed concert(s) to settle")]
    internal static partial void FoundConcertsToSettle(this ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Finished concert {ConcertId}")]
    internal static partial void ConcertFinished(this ILogger logger, int concertId);
}
