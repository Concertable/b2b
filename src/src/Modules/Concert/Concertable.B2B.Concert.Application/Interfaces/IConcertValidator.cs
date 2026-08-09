using Concertable.B2B.Concert.Domain.Entities;
using Reunion.Errors;
using Reunion;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IConcertValidator
{
    UnitResult<ValidationErrors> CanUpdate(ConcertEntity concert, int newTotalTickets);
    UnitResult<ValidationErrors> CanPost(ConcertEntity concert);
}
