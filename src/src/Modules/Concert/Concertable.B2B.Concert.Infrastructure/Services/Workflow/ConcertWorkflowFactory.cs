using Concertable.B2B.Concert.Application.Strategies;
using Concertable.B2B.Concert.Application.Workflow;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow;

internal sealed class ConcertWorkflowFactory : IConcertWorkflowFactory
{
    private readonly IConcertDealStrategyFactory<IConcertWorkflow> strategies;

    public ConcertWorkflowFactory(IConcertDealStrategyFactory<IConcertWorkflow> strategies)
    {
        this.strategies = strategies;
    }

    public IConcertWorkflow Create(DealType type) => strategies.Create(type);
}
