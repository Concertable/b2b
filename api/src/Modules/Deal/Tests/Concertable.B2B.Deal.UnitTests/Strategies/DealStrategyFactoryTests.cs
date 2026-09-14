using Concertable.B2B.Deal.Application.Interfaces;
using Concertable.B2B.Deal.Application.Mappers;
using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Domain.Entities;
using Concertable.B2B.Deal.Infrastructure.Extensions;
using Concertable.B2B.Deal.Infrastructure.Services.Updaters;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Deal.UnitTests.Strategies;

public sealed class DealStrategyFactoryTests
{
    [Theory]
    [MemberData(nameof(Cases))]
    public void Create_DealCase_ResolvesExpectedStrategies(
        DealDto deal,
        DealEntity entity,
        Type expectedMapperType,
        Type expectedUpdaterType)
    {
        var services = new ServiceCollection();
        services.AddDealStrategies();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
        using var scope = provider.CreateScope();

        var mapperFactory = scope.ServiceProvider
            .GetRequiredService<IDealStrategyFactory<IDealMapper>>();
        var updaterFactory = scope.ServiceProvider
            .GetRequiredService<IDealStrategyFactory<IDealUpdater>>();

        Assert.IsType(expectedMapperType, mapperFactory.Create(deal.DealType));
        Assert.IsType(expectedMapperType, mapperFactory.Create(entity.DealType));
        Assert.IsType(expectedUpdaterType, updaterFactory.Create(deal.DealType));
        Assert.IsType(expectedUpdaterType, updaterFactory.Create(entity.DealType));
    }

    [Fact]
    public void AddDealStrategies_RegistersScopedFacadesAndFactory()
    {
        var services = new ServiceCollection();

        services.AddDealStrategies();

        foreach (var serviceType in new[] { typeof(IDealMapper), typeof(IDealUpdater) })
        {
            var descriptor = Assert.Single(
                services,
                candidate => candidate.ServiceType == serviceType && !candidate.IsKeyedService);
            Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        }

        var factory = Assert.Single(
            services,
            candidate => candidate.ServiceType == typeof(IDealStrategyFactory<>));
        Assert.Equal(ServiceLifetime.Scoped, factory.Lifetime);
    }

    [Fact]
    public void Create_StrategiesAreSingletonsAcrossScopes()
    {
        var services = new ServiceCollection();
        services.AddDealStrategies();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();
        var deal = new FlatFeeDealDto();

        var firstFactory = firstScope.ServiceProvider
            .GetRequiredService<IDealStrategyFactory<IDealMapper>>();
        var secondFactory = secondScope.ServiceProvider
            .GetRequiredService<IDealStrategyFactory<IDealMapper>>();

        var first = firstFactory.Create(deal.DealType);
        Assert.Same(first, firstFactory.Create(deal.DealType));
        Assert.Same(first, secondFactory.Create(deal.DealType));
    }

    [Fact]
    public void Apply_MismatchedEntityAndSource_ReturnsFailure()
    {
        var services = new ServiceCollection();
        services.AddDealStrategies();
        services.AddScoped<IDealUpdater, DealUpdater>();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
        using var scope = provider.CreateScope();
        var existing = CreateFlatFeeEntity();

        var result = scope.ServiceProvider
            .GetRequiredService<IDealUpdater>()
            .Apply(existing, new DoorSplitDealDto { ArtistDoorPercent = 50 });

        Assert.True(result.IsFailure);
        Assert.Equal(100, existing.Fee);
    }

    public static TheoryData<DealDto, DealEntity, Type, Type> Cases { get; } = new()
    {
        {
            new FlatFeeDealDto(),
            CreateFlatFeeEntity(),
            typeof(FlatFeeDealMapper),
            typeof(FlatFeeDealUpdater)
        },
        {
            new DoorSplitDealDto(),
            CreateDoorSplitEntity(),
            typeof(DoorSplitDealMapper),
            typeof(DoorSplitDealUpdater)
        },
        {
            new VersusDealDto(),
            CreateVersusEntity(),
            typeof(VersusDealMapper),
            typeof(VersusDealUpdater)
        },
        {
            new VenueHireDealDto(),
            CreateVenueHireEntity(),
            typeof(VenueHireDealMapper),
            typeof(VenueHireDealUpdater)
        }
    };

    private static FlatFeeDealEntity CreateFlatFeeEntity()
    {
        var result = FlatFeeDealEntity.Create(100, PaymentMethod.Cash);
        return result.TryGetValue(out var entity)
            ? entity
            : throw new InvalidOperationException();
    }

    private static DoorSplitDealEntity CreateDoorSplitEntity()
    {
        var result = DoorSplitDealEntity.Create(50, PaymentMethod.Cash);
        return result.TryGetValue(out var entity)
            ? entity
            : throw new InvalidOperationException();
    }

    private static VersusDealEntity CreateVersusEntity()
    {
        var result = VersusDealEntity.Create(100, 50, PaymentMethod.Cash);
        return result.TryGetValue(out var entity)
            ? entity
            : throw new InvalidOperationException();
    }

    private static VenueHireDealEntity CreateVenueHireEntity()
    {
        var result = VenueHireDealEntity.Create(100, PaymentMethod.Cash);
        return result.TryGetValue(out var entity)
            ? entity
            : throw new InvalidOperationException();
    }
}
