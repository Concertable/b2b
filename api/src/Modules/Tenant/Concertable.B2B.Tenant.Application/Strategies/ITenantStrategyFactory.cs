namespace Concertable.B2B.Tenant.Application.Strategies;

internal interface ITenantStrategy;

internal interface ITenantStrategyFactory<TStrategy>
    where TStrategy : class, ITenantStrategy
{
    TStrategy Create(TenantType type);
}
