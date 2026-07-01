using Concertable.B2B.Concert.Domain.Entities;
using FluentResults;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IConcertDraftService
{
    Task<Result<ConcertEntity>> CreateAsync(int bookingId);
}
