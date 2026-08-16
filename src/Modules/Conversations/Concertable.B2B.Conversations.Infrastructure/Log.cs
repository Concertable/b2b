using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Conversations.Infrastructure;

internal static partial class Log
{
    #region ContentReportService

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Content report {Reference} was recorded but its notifications could not be sent")]
    internal static partial void ContentReportNotificationFailed(this ILogger logger, string reference, Exception exception);

    #endregion

    #region ContentReportNotifier

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Content report {Reference} has no reporter email address, so no acknowledgement was sent")]
    internal static partial void ReporterEmailMissing(this ILogger logger, string reference);

    #endregion
}
