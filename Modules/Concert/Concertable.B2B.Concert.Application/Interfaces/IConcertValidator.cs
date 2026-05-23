using Concertable.B2B.Concert.Domain.Entities;
using FluentResults;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IConcertValidator
{
    Result CanUpdate(ConcertEntity concert, int newTotalTickets);
    Result CanPost(ConcertEntity concert);
}
