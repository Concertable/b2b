using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure;

internal static partial class Log
{
    #region Payment processors

    [LoggerMessage(Level = LogLevel.Debug, Message = "Duplicate inbox message {MessageId}; skipping")]
    internal static partial void DuplicateInboxMessage(this ILogger logger, Guid messageId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Concert {ConcertId} not found for ticket sale")]
    internal static partial void ConcertNotFoundForTicketSale(this ILogger logger, int concertId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Settlement outcome received for {ClientReference} on concert {ConcertId}")]
    internal static partial void SettlementWebhookReceived(this ILogger logger, string clientReference, int concertId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Settlement failed for concert {ConcertId}: [{FailureCode}] {FailureMessage}")]
    internal static partial void SettlementPaymentFailed(this ILogger logger, int concertId, string? failureCode, string? failureMessage);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Settlement outcome names concert {ConcertId}, which does not exist; skipping")]
    internal static partial void SettlementOutcomeForUnknownConcert(this ILogger logger, int concertId);

    #endregion

    #region Workflow

    [LoggerMessage(Level = LogLevel.Debug, Message = "Calculated artist share for concert {ConcertId}: {Share}")]
    internal static partial void ArtistShareCalculated(this ILogger logger, int concertId, decimal share);

    [LoggerMessage(Level = LogLevel.Information, Message = "Settling concert {ConcertId} (booking {BookingId}): paying {Amount} GBP from {PayerId} to {PayeeId}")]
    internal static partial void SettlingConcert(this ILogger logger, int concertId, int bookingId, decimal amount, Guid payerId, Guid payeeId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to finish concert {ConcertId}")]
    internal static partial void FailedToFinishConcert(this ILogger logger, int concertId, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Settlement of concert {ConcertId} deferred: party tenant {IncompleteTenantId} tax identity is not complete for its jurisdiction; will retry on the next completion sweep once details are provided")]
    internal static partial void SettlementDeferredPendingTaxCompliance(this ILogger logger, int concertId, Guid incompleteTenantId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Settlement of concert {ConcertId} deferred: supplier tenant {SupplierTenantId} holds no current self-billing agreement, so no self-billed invoice may be raised in their name; will retry on the next completion sweep once the supplier grants or renews consent")]
    internal static partial void SettlementDeferredPendingSelfBillingAgreement(this ILogger logger, int concertId, Guid supplierTenantId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Settlement of concert {ConcertId} deferred: party tenant {UnverifiedTenantId} is not verified; will retry on the next completion sweep once verification is approved")]
    internal static partial void SettlementDeferredPendingVerification(this ILogger logger, int concertId, Guid unverifiedTenantId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to cancel concert {ConcertId}")]
    internal static partial void FailedToCancelConcert(this ILogger logger, int concertId, Exception ex);

    #endregion

    #region ConcertDraftService

    [LoggerMessage(Level = LogLevel.Information, Message = "Creating concert draft for booking {BookingId}")]
    internal static partial void CreatingConcertDraft(this ILogger logger, int bookingId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Concert draft creation failed for booking {BookingId}: artist {ArtistId} has no matching genres for opportunity {OpportunityId}")]
    internal static partial void ConcertDraftCreationFailed(this ILogger logger, int bookingId, int artistId, int opportunityId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Concert draft {ConcertId} created for booking {BookingId} (artist {ArtistId}, venue {VenueId}); notifying users")]
    internal static partial void ConcertDraftCreated(this ILogger logger, int concertId, int bookingId, int artistId, int venueId);

    #endregion

    #region ContractDocument

    [LoggerMessage(Level = LogLevel.Warning, Message = "Contract {ContractId}: stored {Party} drawn signature could not be decoded; rendering without the drawn image")]
    internal static partial void DrawnSignatureDecodeFailed(this ILogger logger, int contractId, string party);

    #endregion

    #region CompletionRunner

    [LoggerMessage(Level = LogLevel.Information, Message = "CompletionRunner: found {Count} ended confirmed concert(s) to settle")]
    internal static partial void FoundConcertsToSettle(this ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not finish concert {ConcertId}: {Code} {Message}")]
    internal static partial void ConcertCompletionRefused(
        this ILogger logger,
        int concertId,
        string code,
        string message);

    [LoggerMessage(Level = LogLevel.Information, Message = "Finished concert {ConcertId}")]
    internal static partial void ConcertFinished(this ILogger logger, int concertId);


    #endregion
}
