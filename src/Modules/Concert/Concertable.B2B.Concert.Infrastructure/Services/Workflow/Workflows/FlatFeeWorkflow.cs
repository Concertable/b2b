using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Capabilities;
using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Workflows;

internal sealed class FlatFeeWorkflow : IConcertWorkflow, IAppliesSimple, IAcceptsCheckout, IAcceptsSimple
{
    public FlatFeeWorkflow(
        SimpleApplyStep apply,
        HoldCheckoutStep acceptCheckout,
        CaptureEscrowAcceptStep accept,
        CreateConcertDraftStep book,
        ReleaseEscrowFinishStep finish,
        RefundEscrowStep cancel)
    {
        this.Apply = apply;
        this.AcceptCheckout = acceptCheckout;
        this.Accept = accept;
        this.Book = book;
        this.Finish = finish;
        this.Cancel = cancel;
    }

    public ContractType Type => ContractType.FlatFee;
    public ISimpleApplyStep Apply { get; }
    public IAcceptCheckoutStep AcceptCheckout { get; }
    public ISimpleAcceptStep Accept { get; }
    public IBookStep Book { get; }
    public IFinishStep Finish { get; }
    public ICancelStep Cancel { get; }
}
