using Concertable.B2B.Concert.Application.Models;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.DataAccess.Application;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Completion;

internal sealed class CompletionRunner : ICompletionRunner
{
    private readonly IConcertRepository concertRepository;
    private readonly IScoped<IConcertWorkflow> workflow;
    private readonly ILogger<CompletionRunner> logger;

    public CompletionRunner(
        IConcertRepository concertRepository,
        IScoped<IConcertWorkflow> workflow,
        ILogger<CompletionRunner> logger)
    {
        this.concertRepository = concertRepository;
        this.workflow = workflow;
        this.logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var concertIds = await concertRepository.GetEndedPendingCompletionIdsAsync(ct);

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
    }
}
