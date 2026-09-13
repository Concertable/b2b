using Concertable.B2B.Tenant.Application.Strategies;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Tenant.Infrastructure.Services.Strategies;

internal sealed class TenantStrategyFactory<TStrategy> : ITenantStrategyFactory<TStrategy>
    where TStrategy : class, ITenantStrategy
{
    private readonly IKeyedServiceProvider services;

    public TenantStrategyFactory(IKeyedServiceProvider services)
    {
        this.services = services;
    }

    public TStrategy Create(TenantType type) =>
        services.GetRequiredKeyedService<TStrategy>(type);
}
