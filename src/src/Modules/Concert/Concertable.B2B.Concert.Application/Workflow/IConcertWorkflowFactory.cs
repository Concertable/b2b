namespace Concertable.B2B.Concert.Application.Workflow;

internal interface IConcertWorkflowFactory
{
    IConcertWorkflow Create(ContractType type);
}
