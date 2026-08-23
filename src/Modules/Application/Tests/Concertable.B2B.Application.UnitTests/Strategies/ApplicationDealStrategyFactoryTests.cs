using Concertable.B2B.Application.Application.Interfaces;
using Concertable.B2B.Application.Application.Renderers;
using Concertable.B2B.Application.Application.Steps;
using Concertable.B2B.Application.Application.Strategies;
using Concertable.B2B.Application.Infrastructure.Extensions;
using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Contracts.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Application.UnitTests;

public sealed class ApplicationDealStrategyFactoryTests
{
    [Fact]
    public void Create_DealTerms_ResolvesEveryDeclaredDealType()
    {
        var expected = new Dictionary<DealType, Type>
        {
            [DealType.FlatFee] = typeof(FlatFeeDealTerms),
            [DealType.DoorSplit] = typeof(DoorSplitDealTerms),
            [DealType.Versus] = typeof(VersusDealTerms),
            [DealType.VenueHire] = typeof(VenueHireDealTerms)
        };
        using var scope = CreateProvider().CreateScope();
        var factory = scope.ServiceProvider
            .GetRequiredService<IApplicationDealStrategyFactory<IDealTerms>>();

        Assert.Equal(Enum.GetValues<DealType>(), expected.Keys.Order());
        foreach (var (dealType, implementationType) in expected)
            Assert.IsType(implementationType, factory.Create(dealType));
    }

    [Fact]
    public void Create_Accept_ResolvesEveryDeclaredDealTypeToItsMethodHeader()
    {
        var expected = new Dictionary<DealType, Type>
        {
            [DealType.FlatFee] = typeof(FlatFeeAccept),
            [DealType.DoorSplit] = typeof(DoorSplitAccept),
            [DealType.Versus] = typeof(VersusAccept),
            [DealType.VenueHire] = typeof(VenueHireAccept)
        };
        using var scope = CreateProvider().CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IAcceptFactory>();

        Assert.Equal(Enum.GetValues<DealType>(), expected.Keys.Order());
        foreach (var (dealType, implementationType) in expected)
            Assert.IsType(implementationType, factory.Create(CreateDeal(dealType)));
    }

    [Fact]
    public void Resolve_FactoriesAreScopedAndStrategiesAreSingletons()
    {
        using var provider = CreateProvider();
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();

        var firstFactory = firstScope.ServiceProvider.GetRequiredService<IAcceptFactory>();
        var sameFactory = firstScope.ServiceProvider.GetRequiredService<IAcceptFactory>();
        var secondFactory = secondScope.ServiceProvider.GetRequiredService<IAcceptFactory>();
        var firstAccept = firstFactory.Create(new FlatFeeDealDto());
        var secondAccept = secondFactory.Create(new FlatFeeDealDto());

        Assert.Same(firstFactory, sameFactory);
        Assert.NotSame(firstFactory, secondFactory);
        Assert.Same(firstAccept, secondAccept);
        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IAcceptFactory>());
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddApplicationDealStrategies();
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static DealDto CreateDeal(DealType dealType) => dealType switch
    {
        DealType.FlatFee => new FlatFeeDealDto(),
        DealType.DoorSplit => new DoorSplitDealDto(),
        DealType.Versus => new VersusDealDto(),
        DealType.VenueHire => new VenueHireDealDto(),
        _ => throw new ArgumentOutOfRangeException(nameof(dealType), dealType, null)
    };
}
