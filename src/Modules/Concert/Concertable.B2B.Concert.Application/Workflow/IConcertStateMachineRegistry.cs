using Concertable.B2B.Concert.Domain.Lifecycle;

namespace Concertable.B2B.Concert.Application.Workflow;

internal interface IConcertStateMachineRegistry
{
    ContractStateMachine Get(ContractType type);
}
