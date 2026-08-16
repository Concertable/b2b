using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Infrastructure.Extensions;
using Reunion.Errors;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Concert.UnitTests.Lifecycle;

public sealed class LifecycleStateMachineTests
{
    private static readonly Transition[] EscrowFundedTransitions =
    [
        new(LifecycleState.Applied, Trigger.Accept, LifecycleState.Accepted),
        new(LifecycleState.Applied, Trigger.Reject, LifecycleState.Rejected),
        new(LifecycleState.Applied, Trigger.Withdraw, LifecycleState.Withdrawn),
        new(LifecycleState.Accepted, Trigger.EscrowPaymentSucceeded, LifecycleState.Booked),
        new(LifecycleState.Accepted, Trigger.EscrowPaymentFailed, LifecycleState.PaymentFailed),
        new(LifecycleState.PaymentFailed, Trigger.EscrowPaymentSucceeded, LifecycleState.Booked),
        new(LifecycleState.Cancelled, Trigger.EscrowPaymentSucceeded, LifecycleState.Cancelled),
        new(LifecycleState.Cancelled, Trigger.EscrowPaymentFailed, LifecycleState.Cancelled),
        new(LifecycleState.CancellationPending, Trigger.EscrowPaymentSucceeded, LifecycleState.CancellationPending),
        new(LifecycleState.CancellationPending, Trigger.EscrowPaymentFailed, LifecycleState.CancellationPending),
        new(LifecycleState.Booked, Trigger.Finish, LifecycleState.Complete),
        new(LifecycleState.Booked, Trigger.Cancel, LifecycleState.CancellationPending),
        new(LifecycleState.CancellationPending, Trigger.RefundSucceeded, LifecycleState.Cancelled),
        new(LifecycleState.CancellationPending, Trigger.RefundFailed, LifecycleState.CancellationFailed),
        new(LifecycleState.CancellationFailed, Trigger.Cancel, LifecycleState.CancellationPending),
        new(LifecycleState.Accepted, Trigger.Withdraw, LifecycleState.CancellationPending),
        new(LifecycleState.Accepted, Trigger.Cancel, LifecycleState.CancellationPending),
        new(LifecycleState.PaymentFailed, Trigger.Withdraw, LifecycleState.CancellationPending),
        new(LifecycleState.PaymentFailed, Trigger.Cancel, LifecycleState.CancellationPending)
    ];

    private static readonly Transition[] DeferredSettlementTransitions =
    [
        new(LifecycleState.Applied, Trigger.Accept, LifecycleState.Accepted),
        new(LifecycleState.Applied, Trigger.Reject, LifecycleState.Rejected),
        new(LifecycleState.Applied, Trigger.Withdraw, LifecycleState.Withdrawn),
        new(LifecycleState.Accepted, Trigger.VerifyPaymentSucceeded, LifecycleState.Booked),
        new(LifecycleState.Accepted, Trigger.VerifyPaymentFailed, LifecycleState.PaymentFailed),
        new(LifecycleState.PaymentFailed, Trigger.VerifyPaymentSucceeded, LifecycleState.Booked),
        new(LifecycleState.PaymentFailed, Trigger.VerifyPaymentFailed, LifecycleState.PaymentFailed),
        new(LifecycleState.Booked, Trigger.Finish, LifecycleState.AwaitingSettlement),
        new(LifecycleState.AwaitingSettlement, Trigger.SettlementPaymentSucceeded, LifecycleState.Complete),
        new(LifecycleState.AwaitingSettlement, Trigger.SettlementPaymentFailed, LifecycleState.SettlementFailed),
        new(LifecycleState.SettlementFailed, Trigger.SettlementPaymentSucceeded, LifecycleState.Complete),
        new(LifecycleState.Booked, Trigger.Cancel, LifecycleState.CancellationPending),
        new(LifecycleState.CancellationPending, Trigger.RefundSucceeded, LifecycleState.Cancelled),
        new(LifecycleState.CancellationPending, Trigger.RefundFailed, LifecycleState.CancellationFailed),
        new(LifecycleState.CancellationFailed, Trigger.Cancel, LifecycleState.CancellationPending),
        new(LifecycleState.Accepted, Trigger.Withdraw, LifecycleState.CancellationPending),
        new(LifecycleState.Accepted, Trigger.Cancel, LifecycleState.CancellationPending),
        new(LifecycleState.PaymentFailed, Trigger.Withdraw, LifecycleState.CancellationPending),
        new(LifecycleState.PaymentFailed, Trigger.Cancel, LifecycleState.CancellationPending)
    ];

    public static TheoryData<DealType> AllDealTypes => new(Enum.GetValues<DealType>());

    private static readonly IConcertStateMachineRegistry Registry = BuildRegistry();

    private static IConcertStateMachineRegistry BuildRegistry()
    {
        var services = new ServiceCollection();
        services.AddConcertDealStrategies();
        return services.BuildServiceProvider().GetRequiredService<IConcertStateMachineRegistry>();
    }

    [Theory]
    [MemberData(nameof(AllDealTypes))]
    public void Registry_ShouldProvideAMachine_ForEveryDealType(DealType dealType)
    {
        Assert.NotNull(Registry.Get(dealType));
    }

    [Theory]
    [MemberData(nameof(AllDealTypes))]
    public void Transitions_DealType_MatchesExactTopology(DealType dealType)
    {
        var expected = Order(ExpectedTransitions(dealType));

        var actual = Order(Registry.Get(dealType).Transitions
            .Select(entry => new Transition(entry.Key.Item1, entry.Key.Item2, entry.Value)));

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(DealType.FlatFee, DealType.VenueHire)]
    [InlineData(DealType.DoorSplit, DealType.Versus)]
    public void Transitions_EquivalentDealTypes_ShareExactTopology(DealType first, DealType second)
    {
        var firstTopology = Order(Registry.Get(first).Transitions
            .Select(entry => new Transition(entry.Key.Item1, entry.Key.Item2, entry.Value)));
        var secondTopology = Order(Registry.Get(second).Transitions
            .Select(entry => new Transition(entry.Key.Item1, entry.Key.Item2, entry.Value)));

        Assert.Equal(firstTopology, secondTopology);
    }

    [Theory]
    [MemberData(nameof(AllDealTypes))]
    public void Next_ShouldReturnConflict_ForEveryUndeclaredPair(DealType dealType)
    {
        var machine = Registry.Get(dealType);
        var undeclared =
            from state in Enum.GetValues<LifecycleState>()
            from trigger in Enum.GetValues<Trigger>()
            where !machine.Transitions.ContainsKey((state, trigger))
            select (state, trigger);

        foreach (var (state, trigger) in undeclared)
        {
            var result = machine.Next(state, trigger);
            Assert.True(result.TryGetError(out var error));
            Assert.Equal(ErrorKind.Conflict, error.Definition.Kind);
        }
    }

    [Theory]
    [MemberData(nameof(AllDealTypes))]
    public void Transitions_ShouldReachEveryDeclaredState_FromApplied(DealType dealType)
    {
        var machine = Registry.Get(dealType);
        var declared = machine.Transitions.Keys.Select(key => key.Item1)
            .Concat(machine.Transitions.Values)
            .ToHashSet();

        var reachable = new HashSet<LifecycleState> { LifecycleState.Applied };
        bool grew;
        do
        {
            grew = false;
            foreach (var ((state, _), next) in machine.Transitions)
                if (reachable.Contains(state) && reachable.Add(next))
                    grew = true;
        } while (grew);

        Assert.Empty(declared.Except(reachable));
    }

    private static IReadOnlyCollection<Transition> ExpectedTransitions(DealType dealType) => dealType switch
    {
        DealType.FlatFee or DealType.VenueHire => EscrowFundedTransitions,
        DealType.DoorSplit or DealType.Versus => DeferredSettlementTransitions,
        _ => throw new ArgumentOutOfRangeException(nameof(dealType), dealType, null)
    };

    private static Transition[] Order(IEnumerable<Transition> transitions) =>
        transitions.OrderBy(transition => transition.From)
            .ThenBy(transition => transition.On)
            .ThenBy(transition => transition.To)
            .ToArray();

    private readonly record struct Transition(LifecycleState From, Trigger On, LifecycleState To);
}
