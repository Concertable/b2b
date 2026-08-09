using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Renderers;
using Concertable.B2B.Concert.Application.Strategies;
using Concertable.B2B.Concert.Infrastructure.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Concert.UnitTests.Strategies;

public sealed class ConcertDealStrategyFactoryTests
{
    [Theory]
    [InlineData(DealType.FlatFee, typeof(FlatFeeDealTerms))]
    [InlineData(DealType.DoorSplit, typeof(DoorSplitDealTerms))]
    [InlineData(DealType.Versus, typeof(VersusDealTerms))]
    [InlineData(DealType.VenueHire, typeof(VenueHireDealTerms))]
    public void Create_DealType_ResolvesExpectedStrategyFromRequestScope(
        DealType dealType,
        Type expectedType)
    {
        var services = new ServiceCollection();
        services.AddConcertDealStrategies();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider
            .GetRequiredService<IConcertDealStrategyFactory<IDealTerms>>();

        var strategy = factory.Create(dealType);

        Assert.IsType(expectedType, strategy);
    }

    [Fact]
    public void Resolve_FactoryLifetime_IsScoped()
    {
        var services = new ServiceCollection();
        services.AddConcertDealStrategies();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();

        var first = firstScope.ServiceProvider
            .GetRequiredService<IConcertDealStrategyFactory<IDealTerms>>();
        var sameScope = firstScope.ServiceProvider
            .GetRequiredService<IConcertDealStrategyFactory<IDealTerms>>();
        var second = secondScope.ServiceProvider
            .GetRequiredService<IConcertDealStrategyFactory<IDealTerms>>();

        Assert.Same(first, sameScope);
        Assert.NotSame(first, second);
        Assert.Throws<InvalidOperationException>(() =>
            provider.GetRequiredService<IConcertDealStrategyFactory<IDealTerms>>());
    }

    [Fact]
    public void Create_SingletonStrategyLifetime_IsSharedAcrossScopes()
    {
        var services = new ServiceCollection();
        services.AddConcertDealStrategies();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();

        var first = firstScope.ServiceProvider
            .GetRequiredService<IConcertDealStrategyFactory<IDealTerms>>()
            .Create(DealType.FlatFee);
        var second = secondScope.ServiceProvider
            .GetRequiredService<IConcertDealStrategyFactory<IDealTerms>>()
            .Create(DealType.FlatFee);

        Assert.Same(first, second);
    }

    [Fact]
    public void Create_ScopedStrategy_UsesCurrentScopeKeyedProvider()
    {
        var services = new ServiceCollection();
        services.AddConcertDealStrategies(strategies =>
        {
            strategies.For(DealType.FlatFee)
                .AddScoped<ITestStrategy, TestStrategy>();
            strategies.RequireExactly<ITestStrategy>(DealType.FlatFee);
        });
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();
        var firstFactory = firstScope.ServiceProvider
            .GetRequiredService<IConcertDealStrategyFactory<ITestStrategy>>();

        var fromFactory = firstFactory.Create(DealType.FlatFee);
        var fromFirstScope = firstScope.ServiceProvider
            .GetRequiredKeyedService<ITestStrategy>(DealType.FlatFee);
        var fromSecondScope = secondScope.ServiceProvider
            .GetRequiredService<IConcertDealStrategyFactory<ITestStrategy>>()
            .Create(DealType.FlatFee);

        Assert.Same(fromFirstScope, fromFactory);
        Assert.NotSame(fromFactory, fromSecondScope);
        Assert.Throws<InvalidOperationException>(() =>
            provider.GetRequiredKeyedService<ITestStrategy>(DealType.FlatFee));
    }

    [Theory]
    [InlineData(typeof(IKeyedServiceProvider))]
    [InlineData(typeof(IConcertDealStrategyFactory<>))]
    [InlineData(typeof(IDealTermsRenderer))]
    [InlineData(typeof(IDealTermsSerializer))]
    [InlineData(typeof(ITermsFingerprintCalculator))]
    public void AddConcertDealStrategies_ScopeCapturingServices_RegistersScoped(Type serviceType)
    {
        var services = new ServiceCollection();

        services.AddConcertDealStrategies();

        var descriptor = Assert.Single(services, candidate => candidate.ServiceType == serviceType);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    private interface ITestStrategy;

    private sealed class TestStrategy : ITestStrategy;
}
