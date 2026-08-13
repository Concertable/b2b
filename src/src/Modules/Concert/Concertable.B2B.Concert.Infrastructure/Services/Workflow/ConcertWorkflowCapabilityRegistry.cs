using System.Collections.Frozen;
using Concertable.B2B.Concert.Application.Workflow;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow;

internal sealed class ConcertWorkflowCapabilityRegistry : IConcertWorkflowCapabilityRegistry
{
    private readonly FrozenDictionary<DealType, Type> workflowTypes;

    public ConcertWorkflowCapabilityRegistry(IReadOnlyDictionary<DealType, Type> workflowTypes)
        => this.workflowTypes = workflowTypes.ToFrozenDictionary();

    public bool Has<TCapability>(DealType dealType) where TCapability : class
        => workflowTypes[dealType].IsAssignableTo(typeof(TCapability));

    public IReadOnlyList<DealType> DealTypesWith<TCapability>() where TCapability : class
        => workflowTypes
            .Where(entry => entry.Value.IsAssignableTo(typeof(TCapability)))
            .Select(entry => entry.Key)
            .ToArray();
}
