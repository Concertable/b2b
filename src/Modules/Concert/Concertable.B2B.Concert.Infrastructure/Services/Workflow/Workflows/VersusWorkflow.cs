using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Capabilities;
using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Workflows;

internal sealed class VersusWorkflow : IConcertWorkflow, IAppliesSimple, IAcceptsCheckout, IAcceptsPaid
{
    public VersusWorkflow(
        SimpleApplyStep apply,
        VerifyCheckoutStep acceptCheckout,
        PaidAcceptStep accept,
        CreateConcertDraftStep book,
        PayoutFinishStep finish,
        RefundEscrowStep cancel)
    {
        this.Apply = apply;
        this.AcceptCheckout = acceptCheckout;
        this.Accept = accept;
        this.Book = book;
        this.Finish = finish;
        this.Cancel = cancel;
    }

    public ContractType Type => ContractType.Versus;
    public ISimpleApplyStep Apply { get; }
    public IAcceptCheckoutStep AcceptCheckout { get; }
    public IPaidAcceptStep Accept { get; }
    public IBookStep Book { get; }
    public IFinishStep Finish { get; }
    public ICancelStep Cancel { get; }
}
