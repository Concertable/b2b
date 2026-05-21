using Concertable.Concert.Application.Workflow;
using Concertable.Concert.Application.Workflow.Capabilities;
using Concertable.Concert.Application.Workflow.Executors;
using Concertable.Shared.Exceptions;

namespace Concertable.Concert.Infrastructure.Services.Workflow.Executors;

internal class VerifyExecutor : IVerifyExecutor
{
    private readonly IWorkflowStateMachine<ApplicationEntity> stateMachine;
    private readonly IConcertWorkflowFactory workflows;

    public VerifyExecutor(IWorkflowStateMachine<ApplicationEntity> stateMachine, IConcertWorkflowFactory workflows)
    {
        this.stateMachine = stateMachine;
        this.workflows = workflows;
    }

    public Task ExecuteAsync(int applicationId)
        => stateMachine.TransitionAsync(applicationId, ConcertStage.Verified, async app =>
        {
            var workflow = workflows.Create(app.ContractType);
            if (workflow is not IVerifies v)
                throw new BadRequestException($"Contract {workflow.Type} does not support Verify");
            await v.Verify.ExecuteAsync(app.Id);
        });
}
