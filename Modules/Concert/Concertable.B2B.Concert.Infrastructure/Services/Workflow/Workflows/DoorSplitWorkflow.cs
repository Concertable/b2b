using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Capabilities;
using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Workflows;

internal sealed class DoorSplitWorkflow : IConcertWorkflow, IAppliesSimple, IAcceptsCheckout, IVerifies, IAcceptsPaid
{
    private readonly ISimpleApplyStep apply;
    private readonly IAcceptCheckoutStep acceptCheckout;
    private readonly IVerifyStep verify;
    private readonly IPaidAcceptStep accept;
    private readonly ISettleStep settle;
    private readonly IFinishStep finish;

    public DoorSplitWorkflow(
        SimpleApplyStep apply,
        DoorSplitAcceptCheckoutStep acceptCheckout,
        DeferredVerifyStep verify,
        PaidAcceptStep accept,
        DeferredSettleStep settle,
        DoorSplitFinishStep finish)
    {
        this.apply = apply;
        this.acceptCheckout = acceptCheckout;
        this.verify = verify;
        this.accept = accept;
        this.settle = settle;
        this.finish = finish;
    }

    public ContractType Type => ContractType.DoorSplit;
    public ISimpleApplyStep Apply => apply;
    public IAcceptCheckoutStep AcceptCheckout => acceptCheckout;
    public IVerifyStep Verify => verify;
    public IPaidAcceptStep Accept => accept;
    public ISettleStep Settle => settle;
    public IFinishStep Finish => finish;
}
