using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Capabilities;
using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Workflows;

internal sealed class VenueHireWorkflow : IConcertWorkflow, IAppliesPaid, IAppliesCheckout, IAcceptsSimple
{
    private readonly IPaidApplyStep apply;
    private readonly IApplyCheckoutStep applyCheckout;
    private readonly ISimpleAcceptStep accept;
    private readonly IBookStep book;
    private readonly IFinishStep finish;

    public VenueHireWorkflow(
        PaidApplyStep apply,
        SetupCheckoutStep applyCheckout,
        DepositEscrowAcceptStep accept,
        CreateConcertDraftStep book,
        ReleaseEscrowFinishStep finish)
    {
        this.apply = apply;
        this.applyCheckout = applyCheckout;
        this.accept = accept;
        this.book = book;
        this.finish = finish;
    }

    public ContractType Type => ContractType.VenueHire;
    public IPaidApplyStep Apply => apply;
    public IApplyCheckoutStep ApplyCheckout => applyCheckout;
    public ISimpleAcceptStep Accept => accept;
    public IBookStep Book => book;
    public IFinishStep Finish => finish;
}
