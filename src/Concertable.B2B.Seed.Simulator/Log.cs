using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Seed.Simulator;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Published {Count} venue events")]
    internal static partial void PublishedVenueEvents(this ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Published {Count} artist events")]
    internal static partial void PublishedArtistEvents(this ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Published {Count} concert events")]
    internal static partial void PublishedConcertEvents(this ILogger logger, int count);
}
