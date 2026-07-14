namespace Concertable.B2B.Concert.Application.Workflow;

internal interface IConcertWorkflowCapabilityRegistry
{
    bool Has<TCapability>(DealType contractType) where TCapability : class;
}
