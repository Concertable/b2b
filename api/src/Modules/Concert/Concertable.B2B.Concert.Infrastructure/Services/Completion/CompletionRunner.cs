using Concertable.B2B.Concert.Application.Models;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.DataAccess.Application;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.DataAccess.Application;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Completion;

internal sealed class CompletionRunner : ICompletionRunner
{
    private readonly IConcertRepository concertRepository;
    private readonly IScoped<IConcertWorkflow> workflow;
    private readonly ITenantScope tenantScope;
    private readonly ILogger<CompletionRunner> logger;

    public CompletionRunner(
        IConcertRepository concertRepository,
        IScoped<IConcertWorkflow> workflow,
        ITenantScope tenantScope,
        ILogger<CompletionRunner> logger)
    {
        this.concertRepository = concertRepository;
        this.workflow = workflow;
        this.tenantScope = tenantScope;
        this.logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var candidates = await concertRepository.GetEndedPendingCompletionAsync(ct);

        logger.FoundConcertsToSettle(candidates.Count);

        foreach (var (concertId, settlementPayeeTenantId) in candidates)
        {
            // Settlement reads the supplier's self-billing agreement, which is single-tenant, so the
            // sweep acts as the payee rather than either party of the concert.
            using var acting = tenantScope.As(settlementPayeeTenantId);
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
