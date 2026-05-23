using FluentResults;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface ICompletionDispatcher
{
    Task<Result> FinishAsync(int concertId);
}
