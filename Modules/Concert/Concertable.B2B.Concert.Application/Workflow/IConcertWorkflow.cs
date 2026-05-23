using Concertable.B2B.Concert.Application.Workflow.Steps;

namespace Concertable.B2B.Concert.Application.Workflow;

internal interface IConcertWorkflow
{
    ContractType Type { get; }
    ISettleStep Settle { get; }
    IFinishStep Finish { get; }
}
