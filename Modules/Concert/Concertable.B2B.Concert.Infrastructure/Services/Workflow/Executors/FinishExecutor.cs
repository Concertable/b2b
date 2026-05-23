using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Executors;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Enums;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Executors;

internal class FinishExecutor : IFinishExecutor
{
    private readonly IWorkflowStateMachine<ConcertEntity> stateMachine;
    private readonly IConcertWorkflowFactory workflows;
    private readonly ILogger<FinishExecutor> logger;

    public FinishExecutor(
        IWorkflowStateMachine<ConcertEntity> stateMachine,
        IConcertWorkflowFactory workflows,
        ILogger<FinishExecutor> logger)
    {
        this.stateMachine = stateMachine;
        this.workflows = workflows;
        this.logger = logger;
    }

    public async Task<Result> ExecuteAsync(int concertId)
    {
        try
        {
            await stateMachine.TransitionAsync(concertId, ConcertStage.Finished, async concert =>
            {
                var workflow = workflows.Create(concert.ContractType);
                await workflow.Finish.ExecuteAsync(concert.Id);
            });
            return Result.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to finish concert {ConcertId}", concertId);
            return Result.Fail(ex.Message);
        }
    }
}
