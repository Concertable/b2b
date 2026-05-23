using Concertable.B2B.Concert.Application.Workflow.Steps;

namespace Concertable.B2B.Concert.Application.Workflow.Capabilities;

internal interface IVerifies
{
    IVerifyStep Verify { get; }
}
