using FluentResults;

namespace Concertable.B2B.Concert.Application.Workflow.Executors;

internal interface ICancelExecutor
{
    Task<Result> CancelAsync(int concertId, CancellationToken ct = default);
}
