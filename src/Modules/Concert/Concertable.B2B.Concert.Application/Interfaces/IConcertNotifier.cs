namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IConcertNotifier
{
    Task ConcertDraftCreatedAsync(string userId, object payload);
}
