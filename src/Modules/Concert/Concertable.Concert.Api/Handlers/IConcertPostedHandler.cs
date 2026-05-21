namespace Concertable.Concert.Api.Handlers;

internal interface IConcertPostedHandler
{
    Task HandleAsync(ConcertPostResponse result);
}
