using Concertable.B2B.Concert.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Completion;

internal sealed class ConcertCompletionRunner(
    IConcertRepository concertRepository,
    ICompletionDispatcher completionDispatcher,
    ILogger<ConcertCompletionRunner> logger) : IConcertCompletionRunner
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        var concertIds = (await concertRepository.GetEndedConfirmedIdsAsync()).ToList();

        logger.FoundConcertsToSettle(concertIds.Count);

        foreach (var concertId in concertIds)
        {
            var result = await completionDispatcher.FinishAsync(concertId);

            if (result.IsFailed)
                logger.ConcertCompletionFailed(concertId, result.Errors);
            else
                logger.ConcertFinished(concertId);
        }
    }
}
