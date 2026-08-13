using Concertable.B2B.Concert.Application.Strategies;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Concert.Infrastructure.Services.Strategies;

internal sealed class ConcertDealStrategyFactory<TStrategy> : IConcertDealStrategyFactory<TStrategy>
    where TStrategy : class
{
    private readonly IKeyedServiceProvider services;

    public ConcertDealStrategyFactory(IKeyedServiceProvider services)
    {
        this.services = services;
    }

    public TStrategy Create(DealType dealType) =>
        services.GetRequiredKeyedService<TStrategy>(dealType);
}
