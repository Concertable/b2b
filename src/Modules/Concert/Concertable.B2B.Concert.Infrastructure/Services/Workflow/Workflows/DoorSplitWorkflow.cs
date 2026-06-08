using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Capabilities;
using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Workflows;

internal sealed class DoorSplitWorkflow : IConcertWorkflow, IAppliesSimple, IAcceptsCheckout, IAcceptsPaid
{
    private readonly ISimpleApplyStep apply;
    private readonly IAcceptCheckoutStep acceptCheckout;
    private readonly IPaidAcceptStep accept;
    private readonly IBookStep book;
    private readonly IFinishStep finish;

    public DoorSplitWorkflow(
        SimpleApplyStep apply,
        VerifyCheckoutStep acceptCheckout,
        PaidAcceptStep accept,
        CreateConcertDraftStep book,
        PayoutFinishStep finish)
    {
        this.apply = apply;
        this.acceptCheckout = acceptCheckout;
        this.accept = accept;
        this.book = book;
        this.finish = finish;
    }

    public ContractType Type => ContractType.DoorSplit;
    public ISimpleApplyStep Apply => apply;
    public IAcceptCheckoutStep AcceptCheckout => acceptCheckout;
    public IPaidAcceptStep Accept => accept;
    public IBookStep Book => book;
    public IFinishStep Finish => finish;
}
