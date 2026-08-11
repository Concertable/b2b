using Concertable.B2B.Concert.Application.Strategies;
using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Capabilities;
using Concertable.B2B.Concert.Infrastructure.Extensions;
using Concertable.B2B.Concert.Infrastructure.Services.Workflow;
using Concertable.B2B.Concert.Infrastructure.Services.Workflow.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Concertable.B2B.Concert.UnitTests.Strategies;

public sealed class ConcertWorkflowCompositionTests
{
    public static TheoryData<DealType> AllDealTypes => new(Enum.GetValues<DealType>());

    public static TheoryData<DealType, Type> WorkflowTypes => new()
    {
        { DealType.FlatFee, typeof(FlatFeeWorkflow) },
        { DealType.DoorSplit, typeof(DoorSplitWorkflow) },
        { DealType.Versus, typeof(VersusWorkflow) },
        { DealType.VenueHire, typeof(VenueHireWorkflow) }
    };

    [Theory]
    [MemberData(nameof(WorkflowTypes))]
    public void AddConcertDealStrategies_DealType_RegistersExactlyOneScopedWorkflow(
        DealType dealType,
        Type workflowType)
    {
        var services = new ServiceCollection();

        services.AddConcertDealStrategies();

        var registration = Assert.Single(
            services,
            descriptor => descriptor.IsKeyedService
                && descriptor.ServiceType == typeof(IConcertWorkflow)
                && Equals(descriptor.ServiceKey, dealType));
        Assert.Equal(workflowType, registration.KeyedImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, registration.Lifetime);
    }

    [Theory]
    [MemberData(nameof(AllDealTypes))]
    public void AddConcertDealStrategies_DealType_RegistersExactlyOneStateMachine(
        DealType dealType)
    {
        var services = new ServiceCollection();
        services.AddConcertDealStrategies();
        var registration = Assert.Single(
            services,
            descriptor => descriptor.ServiceType == typeof(IConcertStateMachineRegistry));
        var registry = Assert.IsType<ConcertStateMachineRegistry>(registration.ImplementationInstance);

        var first = registry.Get(dealType);
        var second = registry.Get(dealType);

        Assert.Same(first, second);
        Assert.NotEmpty(first.Transitions);
    }

    [Theory]
    [InlineData(DealType.FlatFee, true, false, false, true, true, false)]
    [InlineData(DealType.DoorSplit, true, false, false, true, false, true)]
    [InlineData(DealType.Versus, true, false, false, true, false, true)]
    [InlineData(DealType.VenueHire, false, true, true, false, true, false)]
    public void CapabilityRegistry_DealType_PreservesWorkflowCapabilities(
        DealType dealType,
        bool appliesSimple,
        bool appliesPaid,
        bool appliesCheckout,
        bool acceptsCheckout,
        bool acceptsSimple,
        bool acceptsPaid)
    {
        var services = new ServiceCollection();
        services.AddConcertDealStrategies();
        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IConcertWorkflowCapabilityRegistry>();

        Assert.Equal(appliesSimple, registry.Has<IAppliesSimple>(dealType));
        Assert.Equal(appliesPaid, registry.Has<IAppliesPaid>(dealType));
        Assert.Equal(appliesCheckout, registry.Has<IAppliesCheckout>(dealType));
        Assert.Equal(acceptsCheckout, registry.Has<IAcceptsCheckout>(dealType));
        Assert.Equal(acceptsSimple, registry.Has<IAcceptsSimple>(dealType));
        Assert.Equal(acceptsPaid, registry.Has<IAcceptsPaid>(dealType));
    }

    [Fact]
    public void Create_DealType_DelegatesToGenericStrategyFactory()
    {
        var workflow = Mock.Of<IConcertWorkflow>();
        var strategies = new Mock<IConcertDealStrategyFactory<IConcertWorkflow>>();
        strategies.Setup(factory => factory.Create(DealType.Versus)).Returns(workflow);
        var factory = new ConcertWorkflowFactory(strategies.Object);

        var result = factory.Create(DealType.Versus);

        Assert.Same(workflow, result);
        strategies.Verify(candidate => candidate.Create(DealType.Versus), Times.Once);
    }
}
