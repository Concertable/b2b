using Concertable.B2B.Concert.Infrastructure;
using Concertable.DataAccess.Application;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Completion;

internal sealed class ConcertCompletionRunner(
    IConcertRepository concertRepository,
    IScoped<ICompletionDispatcher> completion,
    ILogger<ConcertCompletionRunner> logger) : IConcertCompletionRunner
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        var concertIds = (await concertRepository.GetEndedConfirmedIdsAsync()).ToList();

        logger.FoundConcertsToSettle(concertIds.Count);

        foreach (var concertId in concertIds)
        {
            var result = await completion.RunAsync(d => d.FinishAsync(concertId));

            if (result.IsFailed)
                logger.ConcertCompletionFailed(concertId, result.Errors);
            else
                logger.ConcertFinished(concertId);
        }
    }
}
