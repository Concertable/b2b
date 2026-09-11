using Concertable.Testing.Integration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.IntegrationTests.Fixtures;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddResettables(
        this IServiceCollection services,
        params IResettable[] resettables)
    {
        foreach (var resettable in resettables)
            services.AddSingleton(resettable);
        return services;
    }
}
