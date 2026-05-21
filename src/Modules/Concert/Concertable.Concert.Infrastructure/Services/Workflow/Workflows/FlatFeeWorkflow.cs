using Concertable.Concert.Application.Workflow;
using Concertable.Concert.Application.Workflow.Capabilities;
using Concertable.Concert.Application.Workflow.Steps;
using Concertable.Concert.Infrastructure.Services.Workflow.Steps;

namespace Concertable.Concert.Infrastructure.Services.Workflow.Workflows;

internal sealed class FlatFeeWorkflow : IConcertWorkflow, IAppliesSimple, IAcceptsCheckout, IAcceptsSimple
{
    private readonly ISimpleApplyStep apply;
    private readonly IAcceptCheckoutStep acceptCheckout;
    private readonly ISimpleAcceptStep accept;
    private readonly ISettleStep settle;
    private readonly IFinishStep finish;

    public FlatFeeWorkflow(
        SimpleApplyStep apply,
        FlatFeeAcceptCheckoutStep acceptCheckout,
        FlatFeeAcceptStep accept,
        NoOpSettleStep settle,
        FlatFeeFinishStep finish)
    {
        this.apply = apply;
        this.acceptCheckout = acceptCheckout;
        this.accept = accept;
        this.settle = settle;
        this.finish = finish;
    }

    public ContractType Type => ContractType.FlatFee;
    public ISimpleApplyStep Apply => apply;
    public IAcceptCheckoutStep AcceptCheckout => acceptCheckout;
    public ISimpleAcceptStep Accept => accept;
    public ISettleStep Settle => settle;
    public IFinishStep Finish => finish;
}
