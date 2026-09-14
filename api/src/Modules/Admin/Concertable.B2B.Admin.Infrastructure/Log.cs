using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Admin.Infrastructure;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Granted admin profile UserId={UserId} via {Via}")]
    internal static partial void GrantedAdminProfile(this ILogger logger, Guid userId, string via);
}
