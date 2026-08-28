using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Contracts.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Infrastructure.Services.Strategies;

internal sealed class DealTypeStrategyFactory<TStrategy> : IDealTypeStrategyFactory<TStrategy>
    where TStrategy : class
{
    private readonly IKeyedServiceProvider serviceProvider;

    public DealTypeStrategyFactory(IKeyedServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public TStrategy Create(DealType dealType) =>
        this.serviceProvider.GetRequiredKeyedService<TStrategy>(dealType);
}