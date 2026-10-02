using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.IntegrationTests.Fixtures;

public static class ServiceProviderScopeExtensions
{
    public static async Task<TResult> RunScopedAsync<TResult>(
        this IServiceProvider services,
        Func<IServiceProvider, Task<TResult>> body)
    {
        await using var scope = services.CreateAsyncScope();
        return await body(scope.ServiceProvider);
    }
}
