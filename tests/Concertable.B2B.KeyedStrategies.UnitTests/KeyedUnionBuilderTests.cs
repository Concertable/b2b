using Concertable.B2B.KeyedStrategies;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.KeyedStrategies.UnitTests;

public sealed class KeyedUnionBuilderTests
{
    [Fact]
    public void Build_CompleteCoverage_RegistersEveryKeyedUnionCase()
    {
        var services = new ServiceCollection();

        Configure(services, union =>
        {
            union.Case<IFirstCase>(first => new TestUnion.First(first))
                .Use<FirstCase>(TestKey.First, TestKey.Second);
            union.Case<ISecondCase>(second => new TestUnion.Second(second))
                .Use<SecondCase>(TestKey.Third);
        });

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        Assert.IsType<TestUnion.First>(Create(provider, TestKey.First));
        Assert.IsType<TestUnion.First>(Create(provider, TestKey.Second));
        Assert.IsType<TestUnion.Second>(Create(provider, TestKey.Third));
    }

    [Fact]
    public void Build_MissingKey_ThrowsBeforeRegisteringServices()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            Configure(services, union =>
                union.Case<IFirstCase>(first => new TestUnion.First(first))
                    .Use<FirstCase>(TestKey.First, TestKey.Second)));

        Assert.Contains("Missing: Third", exception.Message);
        Assert.Empty(services);
    }

    [Fact]
    public void Use_SameKeyTwice_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            Configure(new ServiceCollection(), union =>
                union.Case<IFirstCase>(first => new TestUnion.First(first))
                    .Use<FirstCase>(TestKey.First, TestKey.First)));

        Assert.Contains("already has a union case registration for First", exception.Message);
    }

    [Fact]
    public void Use_UndeclaredKey_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            Configure(new ServiceCollection(), union =>
                union.Case<IFirstCase>(first => new TestUnion.First(first))
                    .Use<FirstCase>((TestKey)99)));

        Assert.Contains("99 is not a declared TestKey", exception.Message);
    }

    [Fact]
    public void Case_SameCaseTwice_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            Configure(new ServiceCollection(), union =>
            {
                union.Case<IFirstCase>(first => new TestUnion.First(first));
                union.Case<IFirstCase>(first => new TestUnion.First(first));
            }));

        Assert.Contains("IFirstCase is already a declared union case", exception.Message);
    }

    [Fact]
    public void Build_UninhabitedCase_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            Configure(new ServiceCollection(), union =>
            {
                union.Case<IFirstCase>(first => new TestUnion.First(first))
                    .Use<FirstCase>(TestKey.First, TestKey.Second, TestKey.Third);
                union.Case<ISecondCase>(second => new TestUnion.Second(second));
            }));

        Assert.Contains("Union cases have no key registration for TestUnion: ISecondCase", exception.Message);
    }

    [Fact]
    public void Build_ImplementationInMultipleCases_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            Configure(new ServiceCollection(), union =>
            {
                union.Case<IFirstCase>(first => new TestUnion.First(first))
                    .Use<OverlappingCase>(TestKey.First, TestKey.Second);
                union.Case<ISecondCase>(second => new TestUnion.Second(second))
                    .Use<OverlappingCase>(TestKey.Third);
            }));

        Assert.Contains("OverlappingCase implements multiple cases of TestUnion", exception.Message);
    }

    [Fact]
    public void Build_SameImplementationWithDifferentLifetimes_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            Configure(new ServiceCollection(), union =>
            {
                union.Case<IFirstCase>(first => new TestUnion.First(first))
                    .Use<FirstCase>(TestKey.First)
                    .UseScoped<FirstCase>(TestKey.Second, TestKey.Third);
            }));

        Assert.Contains("FirstCase has conflicting union lifetimes", exception.Message);
    }

    [Fact]
    public void Build_CatalogAlreadyRegistered_ThrowsBeforeAddingSecondRegistrations()
    {
        var services = new ServiceCollection();
        ConfigureComplete(services);
        var count = services.Count;

        var exception = Assert.Throws<InvalidOperationException>(() => ConfigureComplete(services));

        Assert.Contains("A keyed union catalog for TestUnion has already been registered", exception.Message);
        Assert.Equal(count, services.Count);
    }

    private static TestUnion Create(IServiceProvider provider, TestKey key)
    {
        var catalog = provider.GetRequiredService<KeyedUnionCatalog<TestKey, TestUnion>>();
        var value = provider.GetRequiredKeyedService(catalog.GetCaseType(key), key);
        return catalog.Create(key, value);
    }

    private static void ConfigureComplete(IServiceCollection services) =>
        Configure(services, union =>
        {
            union.Case<IFirstCase>(first => new TestUnion.First(first))
                .Use<FirstCase>(TestKey.First, TestKey.Second);
            union.Case<ISecondCase>(second => new TestUnion.Second(second))
                .Use<SecondCase>(TestKey.Third);
        });

    private static void Configure(
        IServiceCollection services,
        Action<KeyedUnionBuilder<TestKey, TestUnion>> configure)
    {
        var builder = new KeyedUnionBuilder<TestKey, TestUnion>(services);
        configure(builder);
        builder.Build();
    }

    private enum TestKey
    {
        First = 1,
        Second = 2,
        Third = 3,
    }

    private interface IFirstCase;

    private interface ISecondCase;

    private sealed class FirstCase : IFirstCase;

    private sealed class SecondCase : ISecondCase;

    private sealed class OverlappingCase : IFirstCase, ISecondCase;

    private abstract record TestUnion
    {
        public sealed record First(IFirstCase Case) : TestUnion;

        public sealed record Second(ISecondCase Case) : TestUnion;
    }
}
