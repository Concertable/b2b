using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Capabilities;
using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Workflows;

internal sealed class FlatFeeWorkflow : IConcertWorkflow, IAppliesSimple, IAcceptsCheckout, IAcceptsSimple
{
    private readonly ISimpleApplyStep apply;
    private readonly IAcceptCheckoutStep acceptCheckout;
    private readonly ISimpleAcceptStep accept;
    private readonly IBookStep book;
    private readonly IFinishStep finish;

    public FlatFeeWorkflow(
        SimpleApplyStep apply,
        HoldCheckoutStep acceptCheckout,
        CaptureEscrowAcceptStep accept,
        CreateConcertDraftStep book,
        ReleaseEscrowFinishStep finish)
    {
        this.apply = apply;
        this.acceptCheckout = acceptCheckout;
        this.accept = accept;
        this.book = book;
        this.finish = finish;
    }

    public ContractType Type => ContractType.FlatFee;
    public ISimpleApplyStep Apply => apply;
    public IAcceptCheckoutStep AcceptCheckout => acceptCheckout;
    public ISimpleAcceptStep Accept => accept;
    public IBookStep Book => book;
    public IFinishStep Finish => finish;
}
