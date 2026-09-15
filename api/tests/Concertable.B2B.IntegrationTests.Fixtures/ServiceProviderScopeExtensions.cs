using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.IntegrationTests.Fixtures;

public static class ServiceProviderScopeExtensions
{
    /// <summary>Runs <paramref name="body"/> in a fresh DI scope and disposes it — the shared scope primitive
    /// for integration tests, so no test hand-rolls <c>CreateScope</c>. Resolve whatever scoped services the
    /// body needs from the supplied provider.</summary>
    public static async Task<TResult> RunScopedAsync<TResult>(
        this IServiceProvider services,
        Func<IServiceProvider, Task<TResult>> body)
    {
        await using var scope = services.CreateAsyncScope();
        return await body(scope.ServiceProvider);
    }
}
