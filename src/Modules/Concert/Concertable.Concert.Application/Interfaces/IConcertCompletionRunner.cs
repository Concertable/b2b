namespace Concertable.Concert.Application.Interfaces;

internal interface IConcertCompletionRunner
{
    Task RunAsync(CancellationToken ct = default);
}
