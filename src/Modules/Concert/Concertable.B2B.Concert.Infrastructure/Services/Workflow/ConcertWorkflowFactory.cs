using Concertable.B2B.Concert.Application.Workflow;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow;

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
