using Microsoft.Extensions.Logging;

namespace Concertable.Concert.Infrastructure.Services.Completion;

internal class ConcertCompletionRunner(
    IConcertRepository concertRepository,
    ICompletionDispatcher completionDispatcher,
    ILogger<ConcertCompletionRunner> logger) : IConcertCompletionRunner
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        var concertIds = (await concertRepository.GetEndedConfirmedIdsAsync()).ToList();

        logger.LogInformation("ConcertCompletionRunner: found {Count} ended confirmed concert(s) to settle", concertIds.Count);

        foreach (var concertId in concertIds)
        {
            var result = await completionDispatcher.FinishAsync(concertId);

            if (result.IsFailed)
                logger.LogError("Failed to finish concert {ConcertId}: {Errors}", concertId, result.Errors);
            else
                logger.LogInformation("Finished concert {ConcertId}", concertId);
        }
    }
}
