using System.Collections.Frozen;
using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Domain.Lifecycle;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow;

internal sealed class ConcertStateMachineRegistry : IConcertStateMachineRegistry
{
    private readonly FrozenDictionary<DealType, ContractStateMachine> machines;

    public ConcertStateMachineRegistry(IReadOnlyDictionary<DealType, ContractStateMachine> machines)
        => this.machines = machines.ToFrozenDictionary();

    public ContractStateMachine Get(DealType type) => machines[type];
}
