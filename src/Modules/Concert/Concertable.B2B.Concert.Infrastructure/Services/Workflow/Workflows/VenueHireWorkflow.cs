using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Capabilities;
using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Workflows;

internal sealed class VenueHireWorkflow : IConcertWorkflow, IAppliesPaid, IAppliesCheckout, IAcceptsSimple
{
    public VenueHireWorkflow(
        PaidApplyStep apply,
        SetupCheckoutStep applyCheckout,
        DepositEscrowAcceptStep accept,
        CreateConcertDraftStep book,
        ReleaseEscrowFinishStep finish,
        RefundEscrowStep cancel)
    {
        this.Apply = apply;
        this.ApplyCheckout = applyCheckout;
        this.Accept = accept;
        this.Book = book;
        this.Finish = finish;
        this.Cancel = cancel;
    }

    public ContractType Type => ContractType.VenueHire;
    public IPaidApplyStep Apply { get; }
    public IApplyCheckoutStep ApplyCheckout { get; }
    public ISimpleAcceptStep Accept { get; }
    public IBookStep Book { get; }
    public IFinishStep Finish { get; }
    public ICancelStep Cancel { get; }
}
