using Concertable.Concert.Application.Workflow;
using Concertable.Concert.Application.Workflow.Capabilities;
using Concertable.Concert.Application.Workflow.Steps;
using Concertable.Concert.Infrastructure.Services.Workflow.Steps;

namespace Concertable.Concert.Infrastructure.Services.Workflow.Workflows;

internal sealed class VersusWorkflow : IConcertWorkflow, IAppliesSimple, IAcceptsCheckout, IVerifies, IAcceptsPaid
{
    private readonly ISimpleApplyStep apply;
    private readonly IAcceptCheckoutStep acceptCheckout;
    private readonly IVerifyStep verify;
    private readonly IPaidAcceptStep accept;
    private readonly ISettleStep settle;
    private readonly IFinishStep finish;

    public VersusWorkflow(
        SimpleApplyStep apply,
        VersusAcceptCheckoutStep acceptCheckout,
        DeferredVerifyStep verify,
        PaidAcceptStep accept,
        DeferredSettleStep settle,
        VersusFinishStep finish)
    {
        this.apply = apply;
        this.acceptCheckout = acceptCheckout;
        this.verify = verify;
        this.accept = accept;
        this.settle = settle;
        this.finish = finish;
    }

    public ContractType Type => ContractType.Versus;
    public ISimpleApplyStep Apply => apply;
    public IAcceptCheckoutStep AcceptCheckout => acceptCheckout;
    public IVerifyStep Verify => verify;
    public IPaidAcceptStep Accept => accept;
    public ISettleStep Settle => settle;
    public IFinishStep Finish => finish;
}
