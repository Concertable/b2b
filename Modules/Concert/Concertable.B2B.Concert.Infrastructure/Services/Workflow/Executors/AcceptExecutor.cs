using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Capabilities;
using Concertable.B2B.Concert.Application.Workflow.Executors;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Enums;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Executors;

internal class AcceptExecutor : IAcceptExecutor
{
    private readonly IWorkflowStateMachine<ApplicationEntity> stateMachine;
    private readonly IConcertWorkflowFactory workflows;
    private readonly IContractResolver contractResolver;

    public AcceptExecutor(
        IWorkflowStateMachine<ApplicationEntity> stateMachine,
        IConcertWorkflowFactory workflows,
        IContractResolver contractResolver)
    {
        this.stateMachine = stateMachine;
        this.workflows = workflows;
        this.contractResolver = contractResolver;
    }

    public Task ExecuteAsync(int applicationId, string? paymentMethodId)
        => stateMachine.TransitionAsync(applicationId, ConcertStage.Accepted, async app =>
        {
            await contractResolver.ResolveByApplicationIdAsync(app.Id);
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
