using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Domain.Lifecycle;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow;

internal sealed class ConcertWorkflowRegistryBuilder
{
    public Dictionary<DealType, Type> WorkflowTypes { get; } = [];
    public Dictionary<DealType, LifecycleStateMachine> StateMachines { get; } = [];

    public void Add(DealType contractType, Type workflowType, LifecycleStateMachine stateMachine)
    {
        WorkflowTypes[contractType] = workflowType;
        StateMachines[contractType] = stateMachine;
    }
}
