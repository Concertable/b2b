using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Concert.Domain.Lifecycle;
using static Concertable.B2B.Concert.Domain.Lifecycle.LifecycleState;
using static Concertable.B2B.Concert.Domain.Lifecycle.Trigger;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow;

internal sealed class ConcertWorkflowBuilder
{
    private readonly DealType dealType;
    private readonly Dictionary<(LifecycleState, Trigger), LifecycleState> transitions = [];
    private readonly HashSet<Type> stepTypes = [];

    public ConcertWorkflowBuilder(DealType dealType)
    {
        this.dealType = dealType;
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
        Add(Cancelled, EscrowPaymentSucceeded, Cancelled);
        Add(Cancelled, EscrowPaymentFailed, Cancelled);
        Add(CancellationPending, EscrowPaymentSucceeded, CancellationPending);
        Add(CancellationPending, EscrowPaymentFailed, CancellationPending);
        return this;
    }

    public ConcertWorkflowBuilder WithVerifiedPayment()
    {
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

    public ConcertWorkflowBuilder WithCancel<TStep>() where TStep : class, ICancelStep
    {
        Add(Booked, Cancel, CancellationPending);
        Add(CancellationPending, RefundSucceeded, Cancelled);
        Add(CancellationPending, RefundFailed, CancellationFailed);
        Add(CancellationFailed, Cancel, CancellationPending);
        return RegisterStep<TStep>();
    }

    public ConcertWorkflowBuilder WithApplicationCancel()
    {
        Add(Accepted, Withdraw, CancellationPending);
        Add(Accepted, Cancel, CancellationPending);
        Add(PaymentFailed, Withdraw, CancellationPending);
        Add(PaymentFailed, Cancel, CancellationPending);
        return this;
    }

    public ConcertWorkflowBuilder WithSettlement()
    {
        Add(AwaitingSettlement, SettlementPaymentSucceeded, Complete);
        Add(AwaitingSettlement, SettlementPaymentFailed, SettlementFailed);
        Add(SettlementFailed, SettlementPaymentSucceeded, Complete);
        return this;
    }

    internal ConcertWorkflowRegistration Build<TWorkflow>()
        where TWorkflow : class, IConcertWorkflow =>
        new(typeof(TWorkflow), new LifecycleStateMachine(transitions), stepTypes.ToArray());

    private void Add(LifecycleState from, Trigger on, LifecycleState to)
    {
        if (!transitions.TryAdd((from, on), to))
            throw new InvalidOperationException($"Duplicate transition for {dealType}: {from} + {on}");
    }

    private ConcertWorkflowBuilder RegisterStep<TStep>() where TStep : class, IConcertStep
    {
        stepTypes.Add(typeof(TStep));
        return this;
    }
}

internal sealed record ConcertWorkflowRegistration(
    Type WorkflowType,
    LifecycleStateMachine StateMachine,
    IReadOnlyCollection<Type> StepTypes);
