using FluentResults;

namespace Concertable.Concert.Application.Interfaces;

internal interface IConcertDraftService
{
    Task<Result<ConcertEntity>> CreateAsync(int bookingId);
}
