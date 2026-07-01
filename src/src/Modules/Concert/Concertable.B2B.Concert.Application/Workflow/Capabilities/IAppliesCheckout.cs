using Concertable.B2B.Concert.Application.Workflow.Steps;

namespace Concertable.B2B.Concert.Application.Workflow.Capabilities;

internal interface IAppliesCheckout
{
    IApplyCheckoutStep ApplyCheckout { get; }
}
