using Concertable.B2B.Concert.Application.Models;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.DataAccess.Application;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Completion;

internal sealed class CompletionRunner : ICompletionRunner
{
    private const int BatchSize = 200;

    private readonly IConcertReadRepository readRepository;
    private readonly IScoped<IConcertWorkflow> workflow;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<CompletionRunner> logger;

    public CompletionRunner(
        IConcertReadRepository readRepository,
        IScoped<IConcertWorkflow> workflow,
        TimeProvider timeProvider,
        ILogger<CompletionRunner> logger)
    {
        this.readRepository = readRepository;
        this.workflow = workflow;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var endedBeforeUtc = timeProvider.GetUtcNow().UtcDateTime;
        var afterId = 0;
        while (true)
        {
            var concertIds = await readRepository.GetEndedPendingCompletionIdsAsync(
                endedBeforeUtc, afterId, BatchSize, ct);
            if (concertIds.Count == 0)
                break;

            logger.FoundConcertsToSettle(concertIds.Count);
            foreach (var concertId in concertIds)
            {
                var result = await workflow.RunAsync(workflow => workflow.CompleteAsync(concertId, ct));

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

            afterId = concertIds[^1];
            if (concertIds.Count < BatchSize)
                break;
        }
    }
}
