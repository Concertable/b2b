using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Workflow.Executors;
using FluentResults;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Dispatchers;

internal sealed class DoorRevenueDispatcher : IDoorRevenueDispatcher
{
    private readonly IDoorRevenueExecutor executor;

    public DoorRevenueDispatcher(IDoorRevenueExecutor executor)
    {
        this.executor = executor;
    }

    public Task<Result> DeclareAsync(int concertId, decimal doorRevenue) => executor.ExecuteAsync(concertId, doorRevenue);
}
