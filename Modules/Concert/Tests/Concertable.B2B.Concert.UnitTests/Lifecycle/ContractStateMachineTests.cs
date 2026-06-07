using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Infrastructure.Extensions;
using Concertable.Kernel.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Concert.UnitTests.Lifecycle;

public sealed class ContractStateMachineTests
{
    public static TheoryData<ContractType> AllContractTypes => new(Enum.GetValues<ContractType>());

    private static readonly IConcertStateMachineRegistry Registry = BuildRegistry();

    private static IConcertStateMachineRegistry BuildRegistry()
    {
        var services = new ServiceCollection();
        services.AddConcertWorkflows();
        return services.BuildServiceProvider().GetRequiredService<IConcertStateMachineRegistry>();
    }

    [Theory]
    [MemberData(nameof(AllContractTypes))]
    public void Registry_ShouldProvideAMachine_ForEveryContractType(ContractType contractType)
    {
        Assert.NotNull(Registry.Get(contractType));
    }

    [Theory]
    [MemberData(nameof(AllContractTypes))]
    public void Next_ShouldAdvance_ForEveryDeclaredRow(ContractType contractType)
    {
        // Arrange
        var machine = Registry.Get(contractType);

        // Act + Assert
        foreach (var ((state, trigger), next) in machine.Transitions)
            Assert.Equal(next, machine.Next(state, trigger));
    }

    [Theory]
    [MemberData(nameof(AllContractTypes))]
    public void Next_ShouldThrowConflict_ForEveryUndeclaredPair(ContractType contractType)
    {
        // Arrange
        var machine = Registry.Get(contractType);
        var undeclared =
            from state in Enum.GetValues<LifecycleState>()
            from trigger in Enum.GetValues<Trigger>()
            where !machine.Transitions.ContainsKey((state, trigger))
            select (state, trigger);

        // Act + Assert
        foreach (var (state, trigger) in undeclared)
            Assert.Throws<ConflictException>(() => machine.Next(state, trigger));
    }

    [Theory]
    [MemberData(nameof(AllContractTypes))]
    public void Transitions_ShouldReachEveryDeclaredState_FromApplied(ContractType contractType)
    {
        // Arrange
        var machine = Registry.Get(contractType);
        var declared = machine.Transitions.Keys.Select(key => key.Item1)
            .Concat(machine.Transitions.Values)
            .ToHashSet();

        // Act
        var reachable = new HashSet<LifecycleState> { LifecycleState.Applied };
        bool grew;
        do
        {
            grew = false;
            foreach (var ((state, _), next) in machine.Transitions)
                if (reachable.Contains(state) && reachable.Add(next))
                    grew = true;
        } while (grew);

        // Assert
        Assert.Empty(declared.Except(reachable));
    }
}
