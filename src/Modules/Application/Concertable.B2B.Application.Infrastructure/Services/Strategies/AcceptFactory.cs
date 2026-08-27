using Concertable.B2B.Application.Application.Steps;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Application.Infrastructure.Services.Strategies;

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