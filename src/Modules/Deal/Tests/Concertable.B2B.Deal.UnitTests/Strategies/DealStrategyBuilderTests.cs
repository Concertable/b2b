using Concertable.B2B.Deal.Infrastructure.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Deal.UnitTests.Strategies;

public sealed class DealStrategyBuilderTests
{
    [Fact]
    public void AddSingleton_DuplicateStrategyForDealType_ThrowsBeforeRegistration()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddDealStrategies(builder =>
            {
                builder.For(DealType.FlatFee)
                    .AddSingleton<ITestStrategy, TestStrategy>()
                    .AddSingleton<ITestStrategy, OtherTestStrategy>();
            }));

        Assert.Contains("ITestStrategy already has a registration for FlatFee", exception.Message);
        Assert.Empty(services);
    }

    [Fact]
    public void Build_MissingDealTypes_ThrowsBeforeRegistration()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddDealStrategies(builder =>
            {
                builder.For(DealType.FlatFee)
                    .AddSingleton<ITestStrategy, TestStrategy>();
            }));

        Assert.Contains("Missing: DoorSplit, Versus, VenueHire", exception.Message);
        Assert.Empty(services);
    }

    [Fact]
    public void Build_ImplementationWithConflictingLifetimes_ThrowsBeforeRegistration()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddDealStrategies(builder =>
            {
                builder.For(DealType.FlatFee)
                    .AddSingleton<ITestStrategy, TestStrategy>();
                builder.For(DealType.DoorSplit)
                    .AddScoped<ITestStrategy, TestStrategy>();
                builder.For(DealType.Versus)
                    .AddScoped<ITestStrategy, TestStrategy>();
                builder.For(DealType.VenueHire)
                    .AddScoped<ITestStrategy, TestStrategy>();
            }));

        Assert.Contains("TestStrategy has conflicting strategy lifetimes: Singleton, Scoped", exception.Message);
        Assert.Empty(services);
    }

    private interface ITestStrategy : IDealStrategy;

    private sealed class TestStrategy : ITestStrategy;

    private sealed class OtherTestStrategy : ITestStrategy;
}
