using Concertable.B2B.Concert.Application.Workflow.Steps;

namespace Concertable.B2B.Concert.Application.Workflow;

internal interface IConcertWorkflow
{
    ContractType Type { get; }
    IBookStep Book { get; }
    IFinishStep Finish { get; }
}
