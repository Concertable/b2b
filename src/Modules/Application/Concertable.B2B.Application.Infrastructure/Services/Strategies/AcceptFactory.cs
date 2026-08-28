using Concertable.B2B.Application.Application.Steps;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Application.Infrastructure.Services.Strategies;

internal sealed class AcceptFactory : IAcceptFactory
{
    private readonly IKeyedServiceProvider serviceProvider;

    public AcceptFactory(IKeyedServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public IAccept Create(DealDto deal) =>
        serviceProvider.GetRequiredKeyedService<IAccept>(deal.DealType);
}