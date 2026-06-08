using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Microsoft.Extensions.DependencyInjection;
using static Concertable.B2B.Concert.Domain.Lifecycle.LifecycleState;
using static Concertable.B2B.Concert.Domain.Lifecycle.Trigger;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow;

internal sealed class ConcertWorkflowBuilder
{
    private readonly ContractType contractType;
    private readonly IServiceCollection services;
    private readonly ConcertWorkflowCatalog catalog;
    private readonly Dictionary<(LifecycleState, Trigger), LifecycleState> transitions = [];
    private Type workflowType = null!;

    public ConcertWorkflowBuilder(ContractType contractType, IServiceCollection services, ConcertWorkflowCatalog catalog)
    {
        this.contractType = contractType;
        this.services = services;
        this.catalog = catalog;
    }

    public ConcertWorkflowBuilder WithApply<TStep>() where TStep : class, IConcertStep
    {
        Add(Applied, Accept, Accepted);
        Add(Applied, Reject, Rejected);
        Add(Applied, Withdraw, Withdrawn);
        return RegisterStep<TStep>();
    }

    public ConcertWorkflowBuilder WithCheckout<TStep>() where TStep : class, IConcertStep => RegisterStep<TStep>();

    public ConcertWorkflowBuilder WithAccept<TStep>() where TStep : class, IConcertStep => RegisterStep<TStep>();

    public ConcertWorkflowBuilder WithEscrowPayment()
    {
        Add(Accepted, EscrowPaymentSucceeded, Booked);
        Add(Accepted, EscrowPaymentFailed, PaymentFailed);
        Add(PaymentFailed, EscrowPaymentSucceeded, Booked);
        return this;
    }

    public ConcertWorkflowBuilder WithVerifiedPayment()
    {
        Add(Applied, VerifyPaymentFailed, Applied);
        Add(Accepted, VerifyPaymentSucceeded, Booked);
        Add(Accepted, VerifyPaymentFailed, PaymentFailed);
        Add(PaymentFailed, VerifyPaymentSucceeded, Booked);
        Add(PaymentFailed, VerifyPaymentFailed, PaymentFailed);
        return this;
    }

    public ConcertWorkflowBuilder WithBook<TStep>() where TStep : class, IBookStep => RegisterStep<TStep>();

    public ConcertWorkflowBuilder WithFinish<TStep>(LifecycleState to) where TStep : class, IFinishStep
    {
        Add(Booked, Finish, to);
        return RegisterStep<TStep>();
    }

    public ConcertWorkflowBuilder WithSettlement()
    {
        Add(AwaitingSettlement, SettlementPaymentSucceeded, Complete);
        Add(AwaitingSettlement, SettlementPaymentFailed, SettlementFailed);
        Add(SettlementFailed, SettlementPaymentSucceeded, Complete);
        return this;
    }

    public ConcertWorkflowBuilder WithWorkflow<TWorkflow>() where TWorkflow : class, IConcertWorkflow
    {
        services.AddKeyedScoped<IConcertWorkflow, TWorkflow>(contractType);
        workflowType = typeof(TWorkflow);
        return this;
    }

    public void Build()
    {
        if (workflowType is null)
            throw new InvalidOperationException($"No workflow registered for {contractType}. Call WithWorkflow<T>().");
        catalog.Add(contractType, workflowType, new ContractStateMachine(transitions));
    }

    private void Add(LifecycleState from, Trigger on, LifecycleState to)
    {
        if (!transitions.TryAdd((from, on), to))
            throw new InvalidOperationException($"Duplicate transition for {contractType}: {from} + {on}");
    }

    private ConcertWorkflowBuilder RegisterStep<TStep>() where TStep : class, IConcertStep
    {
        services.AddScoped<TStep>();
        return this;
    }
}
