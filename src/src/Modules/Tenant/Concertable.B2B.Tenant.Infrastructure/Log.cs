using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Tenant.Infrastructure;

internal static partial class Log
{
    #region VerificationNotifier

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Tenant {TenantId} has no verification contact email, so no review notification was sent")]
    internal static partial void VerificationContactEmailMissing(this ILogger logger, Guid tenantId);

    #endregion
}
