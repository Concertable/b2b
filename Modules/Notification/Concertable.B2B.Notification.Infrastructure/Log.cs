using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Notification.Infrastructure;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "[SignalRNotificationClient] send userId={UserId} event={EventName}")]
    internal static partial void SendingSignalRNotification(this ILogger logger, string userId, string eventName);

    [LoggerMessage(Level = LogLevel.Information, Message = "[NotificationHub] connected userId={UserId} userIdentifier={UserIdentifier} connectionId={ConnectionId}")]
    internal static partial void NotificationHubConnected(this ILogger logger, string? userId, string? userIdentifier, string connectionId);
}
