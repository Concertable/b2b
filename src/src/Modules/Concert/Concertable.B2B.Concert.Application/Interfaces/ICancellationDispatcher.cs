using FluentResults;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface ICancellationDispatcher
{
    Task<Result> CancelAsync(int concertId);
}
