using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Admin.Infrastructure;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Handling CredentialRegisteredEvent UserId={UserId} ClientId={ClientId}")]
    internal static partial void HandlingCredentialRegistered(this ILogger logger, Guid userId, string clientId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Skipped CredentialRegisteredEvent UserId={UserId}: {Reason}")]
    internal static partial void SkippedCredentialRegistered(this ILogger logger, Guid userId, string reason);

    [LoggerMessage(Level = LogLevel.Information, Message = "Granted admin profile UserId={UserId} via {Via}")]
    internal static partial void GrantedAdminProfile(this ILogger logger, Guid userId, string via);
}
