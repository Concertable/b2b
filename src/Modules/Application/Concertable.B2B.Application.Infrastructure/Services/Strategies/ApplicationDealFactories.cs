using Concertable.B2B.Application.Application.Steps;
using Concertable.B2B.Application.Application.Strategies;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Application.Infrastructure.Services.Strategies;

internal sealed class ApplicationDealStrategyFactory<TStrategy> : IApplicationDealStrategyFactory<TStrategy>
    where TStrategy : class
{
    private readonly IKeyedServiceProvider services;

    public ApplicationDealStrategyFactory(IKeyedServiceProvider services)
    {
        this.services = services;
    }

    public TStrategy Create(DealType dealType) =>
        services.GetRequiredKeyedService<TStrategy>(dealType);
}

internal sealed class AcceptFactory : IAcceptFactory
{
    private readonly IKeyedServiceProvider services;

    public AcceptFactory(IKeyedServiceProvider services)
    {
        this.services = services;
    }

    public IAccept Create(DealDto deal) =>
        services.GetRequiredKeyedService<IAccept>(deal.DealType);
}
