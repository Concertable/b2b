using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Tenant.Application.Interfaces;
using Concertable.B2B.Tenant.Application.Strategies;
using Concertable.B2B.Tenant.Infrastructure.Extensions;
using Concertable.B2B.Tenant.Infrastructure.Services.Resolvers;
using Concertable.B2B.Venue.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Concertable.B2B.Tenant.UnitTests.Strategies;

public sealed class TenantStrategyFactoryTests
{
    [Theory]
    [InlineData(TenantType.Venue, typeof(VenueTenantContactResolver))]
    [InlineData(TenantType.Artist, typeof(ArtistTenantContactResolver))]
    public void Create_TenantType_ResolvesExpectedResolver(TenantType type, Type expectedResolverType)
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var factory = scope.ServiceProvider
            .GetRequiredService<ITenantStrategyFactory<ITenantContactResolver>>();

        Assert.IsType(expectedResolverType, factory.Create(type));
    }

    [Fact]
    public void AddTenantStrategies_RegistersScopedFacadeAndFactory()
    {
        var services = new ServiceCollection();

        services.AddTenantStrategies();

        var facade = Assert.Single(
            services,
            candidate => candidate.ServiceType == typeof(ITenantContactResolver) && !candidate.IsKeyedService);
        Assert.Equal(typeof(TenantContactResolver), facade.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, facade.Lifetime);

        var factory = Assert.Single(
            services,
            candidate => candidate.ServiceType == typeof(ITenantStrategyFactory<>));
        Assert.Equal(ServiceLifetime.Scoped, factory.Lifetime);
    }

    [Fact]
    public void AddTenantStrategies_RegistersEveryTenantTypeScoped()
    {
        var services = new ServiceCollection();

        services.AddTenantStrategies();

        foreach (var type in Enum.GetValues<TenantType>())
        {
            var leaf = Assert.Single(
                services,
                candidate => candidate.ServiceType == typeof(ITenantContactResolver)
                    && candidate.IsKeyedService
                    && Equals(candidate.ServiceKey, type));
            Assert.Equal(ServiceLifetime.Scoped, leaf.Lifetime);
        }
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => Mock.Of<IVenueModule>());
        services.AddScoped(_ => Mock.Of<IArtistModule>());
        services.AddTenantStrategies();

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
    }
}
