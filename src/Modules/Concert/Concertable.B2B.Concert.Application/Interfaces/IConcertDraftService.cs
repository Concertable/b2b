using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Application.Errors;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IConcertDraftService
{
    Task<Result<ConcertEntity, CreateConcertDraftError>> CreateAsync(int bookingId);
}
