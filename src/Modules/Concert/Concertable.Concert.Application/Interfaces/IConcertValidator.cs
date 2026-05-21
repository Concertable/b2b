using FluentResults;

namespace Concertable.Concert.Application.Interfaces;

internal interface IConcertValidator
{
    Result CanUpdate(ConcertEntity concert, int newTotalTickets);
    Result CanPost(ConcertEntity concert);
}
