using Concertable.B2B.Deal.Application.Strategies;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Deal.Infrastructure.Services.Strategies;

internal sealed class DealStrategyFactory<TStrategy> : IDealStrategyFactory<TStrategy>
    where TStrategy : class
{
    private readonly IKeyedServiceProvider services;

    public DealStrategyFactory(IKeyedServiceProvider services)
    {
        this.services = services;
    }

    public TStrategy Create(DealType dealType) =>
        services.GetRequiredKeyedService<TStrategy>(dealType);
}
