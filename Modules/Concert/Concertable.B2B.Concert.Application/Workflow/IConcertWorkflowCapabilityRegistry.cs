namespace Concertable.B2B.Concert.Application.Workflow;

internal interface IConcertWorkflowCapabilityRegistry
{
    bool Has<TCapability>(ContractType contractType) where TCapability : class;
}
