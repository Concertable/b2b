using Concertable.B2B.Deal.Application.Interfaces;
using Concertable.B2B.Deal.Application.Mappers;
using Concertable.B2B.Deal.Application.Strategies;
using Concertable.B2B.Deal.Infrastructure.Extensions;
using Concertable.B2B.Deal.Infrastructure.Services.Updaters;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Deal.UnitTests.Strategies;

public sealed class DealStrategyFactoryTests
{
    [Theory]
    [InlineData(DealType.FlatFee, typeof(FlatFeeDealMapper), typeof(FlatFeeDealUpdater))]
    [InlineData(DealType.DoorSplit, typeof(DoorSplitDealMapper), typeof(DoorSplitDealUpdater))]
    [InlineData(DealType.Versus, typeof(VersusDealMapper), typeof(VersusDealUpdater))]
    [InlineData(DealType.VenueHire, typeof(VenueHireDealMapper), typeof(VenueHireDealUpdater))]
    public void Create_DealType_ResolvesExpectedStrategiesFromRequestScope(
        DealType dealType,
        Type expectedMapperType,
        Type expectedUpdaterType)
    {
        var services = new ServiceCollection();
        services.AddDealStrategies();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true
        });
        using var scope = provider.CreateScope();

        var mapper = scope.ServiceProvider
            .GetRequiredService<IDealStrategyFactory<IDealMapper>>()
            .Create(dealType);
        var updater = scope.ServiceProvider
            .GetRequiredService<IDealStrategyFactory<IDealUpdater>>()
            .Create(dealType);

        Assert.IsType(expectedMapperType, mapper);
        Assert.IsType(expectedUpdaterType, updater);
    }

    [Theory]
    [InlineData(typeof(IKeyedServiceProvider))]
    [InlineData(typeof(IDealStrategyFactory<>))]
    [InlineData(typeof(IDealMapper))]
    [InlineData(typeof(IDealUpdater))]
    public void AddDealStrategies_ScopeCapturingServices_RegistersScoped(Type serviceType)
    {
        var services = new ServiceCollection();

        services.AddDealStrategies();

        var descriptor = Assert.Single(
            services,
            candidate => candidate.ServiceType == serviceType && !candidate.IsKeyedService);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void Create_SingletonStrategies_AreSharedAcrossScopes()
    {
        var services = new ServiceCollection();
        services.AddDealStrategies();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true
        });
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();

        var first = firstScope.ServiceProvider
            .GetRequiredService<IDealStrategyFactory<IDealMapper>>()
            .Create(DealType.FlatFee);
        var second = secondScope.ServiceProvider
            .GetRequiredService<IDealStrategyFactory<IDealMapper>>()
            .Create(DealType.FlatFee);

        Assert.Same(first, second);
    }
}
