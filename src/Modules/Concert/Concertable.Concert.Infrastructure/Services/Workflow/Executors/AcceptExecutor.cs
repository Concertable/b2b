using Concertable.Concert.Application.Workflow;
using Concertable.Concert.Application.Workflow.Capabilities;
using Concertable.Concert.Application.Workflow.Executors;
using Concertable.Shared.Exceptions;

namespace Concertable.Concert.Infrastructure.Services.Workflow.Executors;

internal class AcceptExecutor : IAcceptExecutor
{
    private readonly IWorkflowStateMachine<ApplicationEntity> stateMachine;
    private readonly IConcertWorkflowFactory workflows;

    public AcceptExecutor(IWorkflowStateMachine<ApplicationEntity> stateMachine, IConcertWorkflowFactory workflows)
    {
        this.stateMachine = stateMachine;
        this.workflows = workflows;
    }

    public Task ExecuteAsync(int applicationId, string? paymentMethodId)
        => stateMachine.TransitionAsync(applicationId, ConcertStage.Accepted, async app =>
        {
            var workflow = workflows.Create(app.ContractType);
            await (workflow switch
            {
                IAcceptsPaid w when paymentMethodId is not null => w.Accept.ExecuteAsync(app.Id, paymentMethodId),
                IAcceptsPaid => throw new BadRequestException("This contract requires a payment method at acceptance"),
                IAcceptsSimple w => w.Accept.ExecuteAsync(app.Id),
                _ => throw new BadRequestException($"Contract {workflow.Type} does not support Accept")
            });
        });
}
