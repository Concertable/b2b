using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Application.Infrastructure;

internal static partial class Log
{
    #region Verify payment processors

    [LoggerMessage(Level = LogLevel.Debug, Message = "Duplicate inbox message {MessageId}; skipping")]
    internal static partial void DuplicateInboxMessage(this ILogger logger, Guid messageId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Verify outcome received for {ClientReference} on application {ApplicationId}")]
    internal static partial void VerifyWebhookReceived(
        this ILogger logger,
        string clientReference,
        int applicationId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Verify outcome for {ClientReference} is not owned by the venue of application {ApplicationId}; skipping")]
    internal static partial void VerifyOutcomeNotOwnedByVenue(
        this ILogger logger,
        string clientReference,
        int applicationId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Verify payment failed for application {ApplicationId}: [{FailureCode}] {FailureMessage}")]
    internal static partial void VerifyPaymentFailed(
        this ILogger logger,
        int applicationId,
        string failureCode,
        string failureMessage);

    #endregion
}
