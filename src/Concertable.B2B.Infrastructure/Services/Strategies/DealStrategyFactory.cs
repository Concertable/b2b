using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Contracts.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Infrastructure.Services.Strategies;

internal sealed class DealStrategyFactory<TStrategy> : IDealStrategyFactory<TStrategy>
    where TStrategy : class, IDealStrategy
{
    private readonly IKeyedServiceProvider serviceProvider;

    public DealStrategyFactory(IKeyedServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public TStrategy Create(DealType dealType) =>
        serviceProvider.GetRequiredKeyedService<TStrategy>(dealType);
}
