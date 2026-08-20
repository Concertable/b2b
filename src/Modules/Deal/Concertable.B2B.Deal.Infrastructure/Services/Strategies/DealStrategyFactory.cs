using Concertable.B2B.Deal.Application.Strategies;
using Concertable.B2B.Deal.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Deal.Infrastructure.Services.Strategies;

internal sealed class DealStrategyFactory<TStrategy> : IDealStrategyFactory<TStrategy>
    where TStrategy : class, IDealStrategy
{
    private readonly IKeyedServiceProvider services;

    public DealStrategyFactory(IKeyedServiceProvider services)
    {
        this.services = services;
    }

    public TStrategy Create(DealDto deal) =>
        services.GetRequiredKeyedService<TStrategy>(deal.DealType);

    public TStrategy Create(DealEntity entity) =>
        services.GetRequiredKeyedService<TStrategy>(entity.DealType);
}
