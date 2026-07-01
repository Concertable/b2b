using Concertable.B2B.Concert.Application.Workflow;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow;

internal sealed class ConcertWorkflowCapabilityRegistry : IConcertWorkflowCapabilityRegistry
{
    private readonly IReadOnlyDictionary<ContractType, Type> strategyTypes;

    public ConcertWorkflowCapabilityRegistry(IReadOnlyDictionary<ContractType, Type> strategyTypes)
        => this.strategyTypes = strategyTypes;

    public bool Has<TCapability>(ContractType contractType) where TCapability : class
        => strategyTypes[contractType].IsAssignableTo(typeof(TCapability));
}
