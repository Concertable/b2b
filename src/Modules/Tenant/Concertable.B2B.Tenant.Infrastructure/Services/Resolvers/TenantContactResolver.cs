using Concertable.B2B.Tenant.Application.Strategies;

namespace Concertable.B2B.Tenant.Infrastructure.Services.Resolvers;

internal sealed class TenantContactResolver : ITenantContactResolver
{
    private readonly ITenantStrategyFactory<ITenantContactResolver> strategies;

    public TenantContactResolver(ITenantStrategyFactory<ITenantContactResolver> strategies)
    {
        this.strategies = strategies;
    }

    public Task<Option<TenantContact>> ResolveAsync(TenantType type, Guid tenantId, CancellationToken ct = default) =>
        strategies.Create(type).ResolveAsync(type, tenantId, ct);
}
