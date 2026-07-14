using FluentResults;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IDoorRevenueDispatcher
{
    Task<Result> DeclareAsync(int concertId, decimal doorRevenue);
}
