using Concertable.B2B.Concert.Application.Workflow;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow;

internal sealed class ConcertWorkflowCapabilityRegistry : IConcertWorkflowCapabilityRegistry
{
    private readonly IReadOnlyDictionary<DealType, Type> strategyTypes;

    public ConcertWorkflowCapabilityRegistry(IReadOnlyDictionary<DealType, Type> strategyTypes)
        => this.strategyTypes = strategyTypes;

    public bool Has<TCapability>(DealType dealType) where TCapability : class
        => strategyTypes[dealType].IsAssignableTo(typeof(TCapability));
}
