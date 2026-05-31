using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Capabilities;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Dispatchers;

internal class CheckoutDispatcher : ICheckoutDispatcher
{
    private readonly IConcertWorkflowFactory workflows;
    private readonly IContractResolver contractResolver;

    public CheckoutDispatcher(IConcertWorkflowFactory workflows, IContractResolver contractResolver)
    {
        this.workflows = workflows;
        this.contractResolver = contractResolver;
    }

    public async Task<Checkout> ApplyCheckoutAsync(int opportunityId)
    {
        var contract = await contractResolver.ResolveByOpportunityIdAsync(opportunityId);
        var workflow = workflows.Create(contract.ContractType);

        return workflow is IAppliesCheckout w
            ? await w.ApplyCheckout.ExecuteAsync(opportunityId)
            : throw new BadRequestException("This contract does not support a pre-apply checkout");
    }

    public async Task<Checkout> AcceptCheckoutAsync(int applicationId)
    {
        var contract = await contractResolver.ResolveByApplicationIdAsync(applicationId);
        var workflow = workflows.Create(contract.ContractType);

        return workflow is IAcceptsCheckout w
            ? await w.AcceptCheckout.ExecuteAsync(applicationId)
            : throw new BadRequestException("This contract does not support an accept checkout");
    }
}
