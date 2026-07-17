using Concertable.B2B.Concert.Application.Workflow.Executors;
using FluentResults;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface ICompletionDispatcher
{
    Task<Result<SettlementOutcome>> FinishAsync(int concertId);
}
