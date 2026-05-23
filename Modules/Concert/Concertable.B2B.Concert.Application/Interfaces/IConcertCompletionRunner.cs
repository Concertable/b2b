namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IConcertCompletionRunner
{
    Task RunAsync(CancellationToken ct = default);
}
