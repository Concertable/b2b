using FluentResults;

namespace Concertable.Concert.Application.Interfaces;

internal interface ICompletionDispatcher
{
    Task<Result> FinishAsync(int concertId);
}
