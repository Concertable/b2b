using Concertable.Kernel.Identity;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.User.Api;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "UserClaimsController: returning Sub={Sub} Role={Role}")]
    internal static partial void UserClaimsReturned(this ILogger logger, Guid sub, Role role);

    [LoggerMessage(Level = LogLevel.Warning, Message = "UserClaimsController: user not found Sub={Sub}")]
    internal static partial void UserClaimsUserNotFound(this ILogger logger, Guid sub);
}
