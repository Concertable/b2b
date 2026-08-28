using Concertable.B2B.Deal.Application.Strategies;
using Concertable.B2B.Deal.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Deal.Infrastructure.Services.Strategies;

internal sealed class DealStrategyFactory<TStrategy> : IDealStrategyFactory<TStrategy>
    where TStrategy : class, IDealStrategy
{
    private readonly IKeyedServiceProvider serviceProvider;

    public DealStrategyFactory(IKeyedServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public TStrategy Create(DealDto deal) =>
        serviceProvider.GetRequiredKeyedService<TStrategy>(deal.DealType);

    public TStrategy Create(DealEntity entity) =>
        serviceProvider.GetRequiredKeyedService<TStrategy>(entity.DealType);
}
