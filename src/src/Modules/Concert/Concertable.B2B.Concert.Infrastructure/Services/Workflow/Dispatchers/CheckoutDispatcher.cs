using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Capabilities;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Dispatchers;

internal sealed class CheckoutDispatcher : ICheckoutDispatcher
{
    private readonly IConcertWorkflowFactory workflows;
    private readonly IDealResolver dealResolver;

    public CheckoutDispatcher(IConcertWorkflowFactory workflows, IDealResolver dealResolver)
    {
        this.workflows = workflows;
        this.dealResolver = dealResolver;
    }

    public async Task<Checkout> ApplyCheckoutAsync(int opportunityId)
    {
        var deal = await dealResolver.ResolveByOpportunityIdAsync(opportunityId);
        var workflow = workflows.Create(deal.DealType);

        return workflow is IAppliesCheckout w
            ? await w.ApplyCheckout.ExecuteAsync(opportunityId)
            : throw new BadRequestException("This deal does not support a pre-apply checkout");
    }

    public async Task<Checkout> AcceptCheckoutAsync(int applicationId)
    {
        var deal = await dealResolver.ResolveByApplicationIdAsync(applicationId);
        var workflow = workflows.Create(deal.DealType);

        return workflow is IAcceptsCheckout w
            ? await w.AcceptCheckout.ExecuteAsync(applicationId)
            : throw new BadRequestException("This deal does not support an accept checkout");
    }
}
