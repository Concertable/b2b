using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Privacy.Infrastructure;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Subject erasure deferred pending financial obligations SubjectId={SubjectId} RequestId={RequestId}")]
    internal static partial void SubjectErasureDeferred(this ILogger logger, Guid subjectId, Guid requestId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Subject erasure completed SubjectId={SubjectId} RequestId={RequestId}")]
    internal static partial void SubjectErasureCompleted(this ILogger logger, Guid subjectId, Guid requestId);
}
