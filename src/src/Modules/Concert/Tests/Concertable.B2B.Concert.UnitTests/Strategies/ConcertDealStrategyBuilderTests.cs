using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Infrastructure.Extensions;
using Concertable.B2B.Concert.Infrastructure.Services.Workflow.Workflows;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Concert.UnitTests.Strategies;

public sealed class ConcertDealStrategyBuilderTests
{
    [Fact]
    public void AddSingleton_DuplicateStrategyForDealType_ThrowsBeforeRegistration()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddConcertDealStrategies(strategies =>
            {
                strategies.For(DealType.FlatFee)
                    .AddSingleton<ITestStrategy, TestStrategy>()
                    .AddSingleton<ITestStrategy, OtherTestStrategy>();
            }));

        Assert.Contains("ITestStrategy already has a registration for FlatFee", exception.Message);
        Assert.Empty(services);
    }

    [Fact]
    public void RequireAll_MissingDealTypes_ThrowsBeforeRegistration()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddConcertDealStrategies(strategies =>
            {
                strategies.For(DealType.FlatFee)
                    .AddSingleton<ITestStrategy, TestStrategy>();
                strategies.RequireAll<ITestStrategy>();
            }));

        Assert.Contains("Missing: DoorSplit, Versus, VenueHire", exception.Message);
        Assert.Empty(services);
    }

    [Fact]
    public void RequireExactly_UnexpectedDealType_ThrowsBeforeRegistration()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddConcertDealStrategies(strategies =>
            {
                strategies.For(DealType.FlatFee)
                    .AddSingleton<ITestStrategy, TestStrategy>();
                strategies.For(DealType.DoorSplit)
                    .AddSingleton<ITestStrategy, OtherTestStrategy>();
                strategies.RequireExactly<ITestStrategy>(DealType.FlatFee);
            }));

        Assert.Contains("Unexpected: DoorSplit", exception.Message);
        Assert.Empty(services);
    }

    [Fact]
    public void Build_StrategyWithoutCoverageDeclaration_ThrowsBeforeRegistration()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddConcertDealStrategies(strategies =>
                strategies.For(DealType.FlatFee)
                    .AddSingleton<ITestStrategy, TestStrategy>()));

        Assert.Contains("Coverage has not been declared for: ITestStrategy", exception.Message);
        Assert.Empty(services);
    }

    [Fact]
    public void Build_ImplementationWithConflictingLifetimes_ThrowsBeforeRegistration()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddConcertDealStrategies(strategies =>
            {
                strategies.For(DealType.FlatFee)
                    .AddSingleton<ITestStrategy, TestStrategy>();
                strategies.For(DealType.DoorSplit)
                    .AddScoped<ITestStrategy, TestStrategy>();
                strategies.RequireExactly<ITestStrategy>(DealType.FlatFee, DealType.DoorSplit);
            }));

        Assert.Contains("TestStrategy has conflicting strategy lifetimes: Singleton, Scoped", exception.Message);
        Assert.Empty(services);
    }

    [Fact]
    public void AddWorkflow_DuplicateWorkflowForDealType_ThrowsBeforeRegistration()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddConcertDealStrategies(strategies =>
                strategies.For(DealType.FlatFee)
                    .AddWorkflow<FlatFeeWorkflow>(_ => { })
                    .AddWorkflow<FlatFeeWorkflow>(_ => { })));

        Assert.Contains("A workflow has already been registered for FlatFee", exception.Message);
        Assert.Empty(services);
    }

    [Fact]
    public void RequireAll_MissingWorkflow_ThrowsBeforeRegistration()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddConcertDealStrategies(strategies =>
            {
                strategies.For(DealType.FlatFee)
                    .AddWorkflow<FlatFeeWorkflow>(_ => { });
                strategies.RequireAll<IConcertWorkflow>();
            }));

        Assert.Contains("Missing: DoorSplit, Versus, VenueHire", exception.Message);
        Assert.Empty(services);
    }

    private interface ITestStrategy;

    private sealed class TestStrategy : ITestStrategy;

    private sealed class OtherTestStrategy : ITestStrategy;
}
