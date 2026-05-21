namespace Concertable.Concert.Application.Workflow;

internal interface IConcertWorkflowFactory
{
    IConcertWorkflow Create(ContractType type);
}
