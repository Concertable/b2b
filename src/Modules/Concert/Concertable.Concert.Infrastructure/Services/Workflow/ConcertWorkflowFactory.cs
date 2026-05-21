using Concertable.Concert.Application.Workflow;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Concert.Infrastructure.Services.Workflow;

internal sealed class ConcertWorkflowFactory : IConcertWorkflowFactory
{
    private readonly IKeyedServiceProvider serviceProvider;

    public ConcertWorkflowFactory(IKeyedServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public IConcertWorkflow Create(ContractType type) =>
        serviceProvider.GetRequiredKeyedService<IConcertWorkflow>(type);
}
