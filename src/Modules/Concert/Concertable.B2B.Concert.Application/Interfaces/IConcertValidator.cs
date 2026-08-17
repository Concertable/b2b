using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Reunion.Validation;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IConcertValidator
{
    ValidationResult CanUpdate(ConcertEntity concert, int newTotalTickets);
    ValidationResult CanPost(ConcertEntity concert, LifecycleState applicationState);
}
