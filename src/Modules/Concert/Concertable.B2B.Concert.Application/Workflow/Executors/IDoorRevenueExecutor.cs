using FluentResults;

namespace Concertable.B2B.Concert.Application.Workflow.Executors;

internal interface IDoorRevenueExecutor
{
    Task<Result> ExecuteAsync(int concertId, decimal doorRevenue);
}
