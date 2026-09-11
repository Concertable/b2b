using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Web;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception occurred")]
    internal static partial void UnhandledException(this ILogger logger, Exception exception);
}
