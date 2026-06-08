using System.Collections.Frozen;
using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Domain.Lifecycle;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow;

internal sealed class ConcertStateMachineRegistry : IConcertStateMachineRegistry
{
    private readonly FrozenDictionary<ContractType, ContractStateMachine> machines;

    public ConcertStateMachineRegistry(IReadOnlyDictionary<ContractType, ContractStateMachine> machines)
        => this.machines = machines.ToFrozenDictionary();

    public ContractStateMachine Get(ContractType type) => machines[type];
}
