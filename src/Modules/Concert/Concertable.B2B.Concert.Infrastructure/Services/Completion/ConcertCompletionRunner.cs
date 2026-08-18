using Concertable.B2B.Concert.Application.Models;
using Concertable.B2B.Concert.Application.Executors;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.DataAccess.Application;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Completion;

internal sealed class ConcertCompletionRunner(
    IConcertRepository concertRepository,
    IScoped<ICompleteExecutor> completion,
    ILogger<ConcertCompletionRunner> logger) : IConcertCompletionRunner
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        var concertIds = await concertRepository.GetEndedPendingCompletionIdsAsync(ct);

        logger.FoundConcertsToSettle(concertIds.Count);

        foreach (var concertId in concertIds)
        {
            var result = await completion.RunAsync(executor => executor.CompleteAsync(concertId, ct));

            if (result.TryGetError(out var error))
                logger.ConcertCompletionRefused(
                    concertId,
                    error.Definition.Code,
                    error.Definition.Message);
            else
            {
                result.TryGetValue(out var outcome);
                if (outcome == SettlementOutcome.Settled)
                    logger.ConcertFinished(concertId);
            }
        }
    }
}
