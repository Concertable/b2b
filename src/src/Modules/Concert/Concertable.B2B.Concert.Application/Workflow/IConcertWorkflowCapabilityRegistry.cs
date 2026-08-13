namespace Concertable.B2B.Concert.Application.Workflow;

internal interface IConcertWorkflowCapabilityRegistry
{
    bool Has<TCapability>(DealType dealType) where TCapability : class;

    IReadOnlyList<DealType> DealTypesWith<TCapability>() where TCapability : class;
}
