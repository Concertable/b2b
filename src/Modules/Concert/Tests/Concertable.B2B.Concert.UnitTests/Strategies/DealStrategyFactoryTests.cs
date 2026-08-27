using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Resolvers;
using Concertable.B2B.Concert.Application.Strategies;
using Concertable.B2B.Concert.Infrastructure.Extensions;
using Concertable.B2B.Concert.Infrastructure.Services.Settlement;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Concertable.B2B.Concert.UnitTests;

public sealed class DealStrategyFactoryTests
{
    [Theory]
    [InlineData(DealType.FlatFee, typeof(VenuePaysArtistDealPayeeResolver))]
    [InlineData(DealType.DoorSplit, typeof(VenuePaysArtistDealPayeeResolver))]
    [InlineData(DealType.Versus, typeof(VenuePaysArtistDealPayeeResolver))]
    [InlineData(DealType.VenueHire, typeof(ArtistPaysVenueDealPayeeResolver))]
    public void Create_DealPayeeType_ResolvesExpectedStrategyFromRequestScope(
        DealType dealType,
        Type expectedType)
    {
        var services = CreateServices();
        services.AddConcertDealStrategies();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true
        });
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider
            .GetRequiredService<IDealStrategyFactory<IDealPayeeResolver>>();

        var strategy = factory.Create(dealType);

        Assert.IsType(expectedType, strategy);
    }

    [Theory]
    [InlineData(DealType.FlatFee, typeof(FlatFeeSettlementAmount))]
    [InlineData(DealType.DoorSplit, typeof(DoorSplitSettlementAmount))]
    [InlineData(DealType.Versus, typeof(VersusSettlementAmount))]
    [InlineData(DealType.VenueHire, typeof(VenueHireSettlementAmount))]
    public void Create_SettlementAmountType_ResolvesExpectedStrategyFromRequestScope(
        DealType dealType,
        Type expectedType)
    {
        var services = CreateServices();
        services.AddConcertDealStrategies();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true
        });
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider
            .GetRequiredService<IDealStrategyFactory<ISettlementAmountResolver>>();

        var strategy = factory.Create(dealType);

        Assert.IsType(expectedType, strategy);
    }

    [Fact]
    public void Resolve_FactoryLifetime_IsScoped()
    {
        var services = CreateServices();
        services.AddConcertDealStrategies();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true
        });
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();

        var first = firstScope.ServiceProvider
            .GetRequiredService<IDealStrategyFactory<IDealPayeeResolver>>();
        var sameScope = firstScope.ServiceProvider
            .GetRequiredService<IDealStrategyFactory<IDealPayeeResolver>>();
        var second = secondScope.ServiceProvider
            .GetRequiredService<IDealStrategyFactory<IDealPayeeResolver>>();

        Assert.Same(first, sameScope);
        Assert.NotSame(first, second);
        Assert.Throws<InvalidOperationException>(() =>
            provider.GetRequiredService<IDealStrategyFactory<IDealPayeeResolver>>());
    }

    [Fact]
    public void Create_SingletonStrategyLifetime_IsSharedAcrossScopes()
    {
        var services = CreateServices();
        services.AddConcertDealStrategies();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true
        });
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();

        var first = firstScope.ServiceProvider
            .GetRequiredService<IDealStrategyFactory<IDealPayeeResolver>>()
            .Create(DealType.FlatFee);
        var second = secondScope.ServiceProvider
            .GetRequiredService<IDealStrategyFactory<IDealPayeeResolver>>()
            .Create(DealType.FlatFee);

        Assert.Same(first, second);
    }

    [Fact]
    public void Create_ScopedStrategy_UsesCurrentScopeKeyedProvider()
    {
        var services = CreateServices();
        services.AddConcertDealStrategies(strategies =>
        {
            strategies.For(DealType.FlatFee)
                .AddScoped<ITestStrategy, TestStrategy>();
            strategies.RequireExactly<ITestStrategy>(DealType.FlatFee);
        });
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true
        });
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();
        var firstFactory = firstScope.ServiceProvider
            .GetRequiredService<IDealStrategyFactory<ITestStrategy>>();

        var fromFactory = firstFactory.Create(DealType.FlatFee);
        var fromFirstScope = firstScope.ServiceProvider
            .GetRequiredKeyedService<ITestStrategy>(DealType.FlatFee);
        var fromSecondScope = secondScope.ServiceProvider
            .GetRequiredService<IDealStrategyFactory<ITestStrategy>>()
            .Create(DealType.FlatFee);

        Assert.Same(fromFirstScope, fromFactory);
        Assert.NotSame(fromFactory, fromSecondScope);
        Assert.Throws<InvalidOperationException>(() =>
            provider.GetRequiredKeyedService<ITestStrategy>(DealType.FlatFee));
    }

    [Theory]
    [InlineData(typeof(IKeyedServiceProvider))]
    [InlineData(typeof(IDealStrategyFactory<>))]
    [InlineData(typeof(IDealPayeeResolver))]
    [InlineData(typeof(ISettlementAmountResolver))]
    public void AddConcertDealStrategies_ScopeCapturingServices_RegistersScoped(Type serviceType)
    {
        var services = CreateServices();

        services.AddConcertDealStrategies();

        var descriptor = Assert.Single(
            services,
            candidate => candidate.ServiceType == serviceType && !candidate.IsKeyedService);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    private static ServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => Mock.Of<IConcertRepository>());
        return services;
    }

    private interface ITestStrategy;

    private sealed class TestStrategy : ITestStrategy;
}
