using Concertable.B2B.KeyedStrategies;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.KeyedStrategies.UnitTests;

public sealed class KeyedStrategyBuilderTests
{
    [Fact]
    public void Build_CompleteCoverage_RegistersEveryKeyedStrategy()
    {
        var services = new ServiceCollection();

        Configure(services, strategies =>
        {
            strategies.For(TestKey.First).AddSingleton<ITestStrategy, FirstStrategy>();
            strategies.For(TestKey.Second).AddSingleton<ITestStrategy, SecondStrategy>();
            strategies.RequireAll<ITestStrategy>();
        });

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        Assert.IsType<FirstStrategy>(provider.GetRequiredKeyedService<ITestStrategy>(TestKey.First));
        Assert.IsType<SecondStrategy>(provider.GetRequiredKeyedService<ITestStrategy>(TestKey.Second));
    }

    [Fact]
    public void Build_RequiredKeyNotRegistered_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            Configure(new ServiceCollection(), strategies =>
            {
                strategies.For(TestKey.First).AddSingleton<ITestStrategy, FirstStrategy>();
                strategies.RequireAll<ITestStrategy>();
            }));

        Assert.Contains("Missing: Second", exception.Message);
    }

    [Fact]
    public void Build_StrategyRegisteredWithoutDeclaredCoverage_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            Configure(new ServiceCollection(), strategies =>
                strategies.For(TestKey.First).AddSingleton<ITestStrategy, FirstStrategy>()));

        Assert.Contains("Coverage has not been declared for: ITestStrategy", exception.Message);
    }

    [Fact]
    public void Build_KeyOutsideRequireExactly_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            Configure(new ServiceCollection(), strategies =>
            {
                strategies.For(TestKey.First).AddSingleton<ITestStrategy, FirstStrategy>();
                strategies.For(TestKey.Second).AddSingleton<ITestStrategy, SecondStrategy>();
                strategies.RequireExactly<ITestStrategy>(TestKey.First);
            }));

        Assert.Contains("Unexpected: Second", exception.Message);
    }

    [Fact]
    public void Build_SameImplementationRegisteredWithDifferentLifetimes_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            Configure(new ServiceCollection(), strategies =>
            {
                strategies.For(TestKey.First).AddSingleton<ITestStrategy, FirstStrategy>();
                strategies.For(TestKey.Second).AddScoped<ITestStrategy, FirstStrategy>();
                strategies.RequireAll<ITestStrategy>();
            }));

        Assert.Contains("conflicting strategy lifetimes", exception.Message);
    }

    [Fact]
    public void Add_SameKeyAndStrategyTwice_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            Configure(new ServiceCollection(), strategies =>
                strategies.For(TestKey.First)
                    .AddSingleton<ITestStrategy, FirstStrategy>()
                    .AddSingleton<ITestStrategy, SecondStrategy>()));

        Assert.Contains("ITestStrategy already has a registration for First", exception.Message);
    }

    [Fact]
    public void RequireExactly_SameKeyTwice_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            Configure(new ServiceCollection(), strategies =>
                strategies.RequireExactly<ITestStrategy>(TestKey.First, TestKey.First)));

        Assert.Contains("duplicate TestKey values", exception.Message);
    }

    [Fact]
    public void RequireExactly_DeclaredTwiceForSameStrategy_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            Configure(new ServiceCollection(), strategies =>
            {
                strategies.RequireAll<ITestStrategy>();
                strategies.RequireAll<ITestStrategy>();
            }));

        Assert.Contains("has already been declared", exception.Message);
    }

    [Fact]
    public void RequireExactly_UndeclaredKey_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            Configure(new ServiceCollection(), strategies =>
                strategies.RequireExactly<ITestStrategy>((TestKey)99)));

        Assert.Contains("undeclared TestKey values: 99", exception.Message);
    }

    [Fact]
    public void For_UndeclaredKey_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            Configure(new ServiceCollection(), strategies => strategies.For((TestKey)99)));

        Assert.Contains("99 is not a declared TestKey", exception.Message);
    }

    private static void Configure(IServiceCollection services, Action<KeyedStrategyBuilder<TestKey>> configure)
    {
        var builder = new KeyedStrategyBuilder<TestKey>(services);
        configure(builder);
        builder.Build();
    }

    private enum TestKey
    {
        First = 1,
        Second = 2,
    }

    private interface ITestStrategy;

    private sealed class FirstStrategy : ITestStrategy;

    private sealed class SecondStrategy : ITestStrategy;
}
