using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Privacy.Infrastructure;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Subject erasure deferred pending financial obligations SubjectId={SubjectId} RequestId={RequestId}")]
    internal static partial void SubjectErasureDeferred(this ILogger logger, Guid subjectId, Guid requestId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Subject erasure completed SubjectId={SubjectId} RequestId={RequestId}")]
    internal static partial void SubjectErasureCompleted(this ILogger logger, Guid subjectId, Guid requestId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Deferred subject-erasure sweep started Deferred={Deferred}")]
    internal static partial void DeferredErasureSweepStarted(this ILogger logger, int deferred);

    [LoggerMessage(Level = LogLevel.Error, Message = "Deferred subject erasure failed SubjectId={SubjectId} RequestId={RequestId}")]
    internal static partial void DeferredErasureFailed(this ILogger logger, Exception exception, Guid subjectId, Guid requestId);
}
