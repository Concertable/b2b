using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Domain.Lifecycle;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow;

internal sealed class ConcertWorkflowCatalog
{
    public Dictionary<ContractType, Type> WorkflowTypes { get; } = [];
    public Dictionary<ContractType, ContractStateMachine> StateMachines { get; } = [];

    public void Add(ContractType contractType, Type workflowType, ContractStateMachine stateMachine)
    {
        WorkflowTypes[contractType] = workflowType;
        StateMachines[contractType] = stateMachine;
    }
}
