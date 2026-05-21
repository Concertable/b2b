using Concertable.Concert.Application.Workflow.Steps;

namespace Concertable.Concert.Application.Workflow;

internal interface IConcertWorkflow
{
    ContractType Type { get; }
    ISettleStep Settle { get; }
    IFinishStep Finish { get; }
}
