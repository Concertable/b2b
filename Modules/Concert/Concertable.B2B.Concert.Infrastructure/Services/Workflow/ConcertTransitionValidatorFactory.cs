using Concertable.B2B.Concert.Application.Workflow;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow;

internal sealed class ConcertTransitionValidatorFactory : IConcertTransitionValidatorFactory
{
    private readonly IKeyedServiceProvider serviceProvider;

    public ConcertTransitionValidatorFactory(IKeyedServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public IConcertTransitionValidator Create(ContractType contractType) =>
        serviceProvider.GetRequiredKeyedService<IConcertTransitionValidator>(contractType);
}
