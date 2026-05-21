using Concertable.Concert.Application.Workflow;
using Concertable.Concert.Application.Workflow.Capabilities;
using Concertable.Concert.Application.Workflow.Steps;
using Concertable.Concert.Infrastructure.Services.Workflow.Steps;

namespace Concertable.Concert.Infrastructure.Services.Workflow.Workflows;

internal sealed class VenueHireWorkflow : IConcertWorkflow, IAppliesPaid, IAppliesCheckout, IAcceptsSimple
{
    private readonly IPaidApplyStep apply;
    private readonly IApplyCheckoutStep applyCheckout;
    private readonly ISimpleAcceptStep accept;
    private readonly ISettleStep settle;
    private readonly IFinishStep finish;

    public VenueHireWorkflow(
        PaidApplyStep apply,
        VenueHireApplyCheckoutStep applyCheckout,
        VenueHireAcceptStep accept,
        NoOpSettleStep settle,
        VenueHireFinishStep finish)
    {
        this.apply = apply;
        this.applyCheckout = applyCheckout;
        this.accept = accept;
        this.settle = settle;
        this.finish = finish;
    }

    public ContractType Type => ContractType.VenueHire;
    public IPaidApplyStep Apply => apply;
    public IApplyCheckoutStep ApplyCheckout => applyCheckout;
    public ISimpleAcceptStep Accept => accept;
    public ISettleStep Settle => settle;
    public IFinishStep Finish => finish;
}
