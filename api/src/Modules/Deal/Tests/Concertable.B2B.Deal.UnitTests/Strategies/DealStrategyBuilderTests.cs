using Concertable.B2B.Infrastructure.Services.Strategies;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Deal.UnitTests.Strategies;

public sealed class DealStrategyBuilderTests
{
    [Fact]
    public void AddSingleton_DuplicateStrategyForDealType_ThrowsBeforeRegistration()
    {
        var services = new ServiceCollection();
        var builder = new DealStrategyBuilder(services);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            builder.For(DealType.FlatFee)
                .AddSingleton<ITestStrategy, TestStrategy>()
                .AddSingleton<ITestStrategy, OtherTestStrategy>());

        Assert.Contains("ITestStrategy already has a registration for FlatFee", exception.Message);
        Assert.Empty(services);
    }

    [Fact]
    public void Build_MissingDealTypes_ThrowsBeforeRegistration()
    {
        var services = new ServiceCollection();
        var builder = new DealStrategyBuilder(services);
        builder.For(DealType.FlatFee)
            .AddSingleton<ITestStrategy, TestStrategy>();

        var exception = Assert.Throws<InvalidOperationException>(builder.Build);

        Assert.Contains("Missing: DoorSplit, Versus, VenueHire", exception.Message);
        Assert.Empty(services);
    }

    [Fact]
    public void Build_ImplementationWithConflictingLifetimes_ThrowsBeforeRegistration()
    {
        var services = new ServiceCollection();
        var builder = new DealStrategyBuilder(services);
        builder.For(DealType.FlatFee)
            .AddSingleton<ITestStrategy, TestStrategy>();
        builder.For(DealType.DoorSplit)
            .AddScoped<ITestStrategy, TestStrategy>();
        builder.For(DealType.Versus)
            .AddScoped<ITestStrategy, TestStrategy>();
        builder.For(DealType.VenueHire)
            .AddScoped<ITestStrategy, TestStrategy>();

        var exception = Assert.Throws<InvalidOperationException>(builder.Build);

        Assert.Contains("TestStrategy has conflicting strategy lifetimes: Singleton, Scoped", exception.Message);
        Assert.Empty(services);
    }

    private interface ITestStrategy : IDealStrategy;

    private sealed class TestStrategy : ITestStrategy;

    private sealed class OtherTestStrategy : ITestStrategy;
}
