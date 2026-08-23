using Concertable.B2B.Application.Infrastructure.Extensions;
using Concertable.B2B.Deal.Contracts.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Application.UnitTests;

public sealed class ApplicationDealStrategyBuilderTests
{
    [Fact]
    public void AddSingleton_DuplicateStrategyForDealType_ThrowsBeforeRegistration()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddApplicationDealStrategies(strategies =>
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
            services.AddApplicationDealStrategies(strategies =>
            {
                strategies.For(DealType.FlatFee)
                    .AddSingleton<ITestStrategy, TestStrategy>();
                strategies.RequireAll<ITestStrategy>();
            }));

        Assert.Contains("Missing: DoorSplit, Versus, VenueHire", exception.Message);
        Assert.Empty(services);
    }

    [Fact]
    public void Build_StrategyWithoutCoverageDeclaration_ThrowsBeforeRegistration()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddApplicationDealStrategies(strategies =>
                strategies.For(DealType.FlatFee)
                    .AddSingleton<ITestStrategy, TestStrategy>()));

        Assert.Contains("Coverage has not been declared for: ITestStrategy", exception.Message);
        Assert.Empty(services);
    }

    private interface ITestStrategy;

    private sealed class TestStrategy : ITestStrategy;

    private sealed class OtherTestStrategy : ITestStrategy;
}
