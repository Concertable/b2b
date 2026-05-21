using Concertable.Concert.Application.Responses;
using Concertable.Concert.Application.Workflow;
using Concertable.Concert.Application.Workflow.Capabilities;
using Concertable.Shared.Exceptions;

namespace Concertable.Concert.Infrastructure.Services.Workflow.Dispatchers;

internal class CheckoutDispatcher : ICheckoutDispatcher
{
    private readonly IConcertWorkflowFactory workflows;
    private readonly IContractLoader contractLoader;

    public CheckoutDispatcher(IConcertWorkflowFactory workflows, IContractLoader contractLoader)
    {
        this.workflows = workflows;
        this.contractLoader = contractLoader;
    }

    public async Task<Checkout> ApplyCheckoutAsync(int opportunityId)
    {
        var contract = await contractLoader.LoadByOpportunityIdAsync(opportunityId);
        var workflow = workflows.Create(contract.ContractType);

        return workflow is IAppliesCheckout w
            ? await w.ApplyCheckout.ExecuteAsync(opportunityId)
            : throw new BadRequestException("This contract does not support a pre-apply checkout");
    }

    public async Task<Checkout> AcceptCheckoutAsync(int applicationId)
    {
        var contract = await contractLoader.LoadByApplicationIdAsync(applicationId);
        var workflow = workflows.Create(contract.ContractType);

        return workflow is IAcceptsCheckout w
            ? await w.AcceptCheckout.ExecuteAsync(applicationId)
            : throw new BadRequestException("This contract does not support an accept checkout");
    }
}
