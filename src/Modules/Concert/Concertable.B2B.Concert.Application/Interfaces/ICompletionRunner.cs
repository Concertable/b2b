namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface ICompletionRunner
{
    Task RunAsync(CancellationToken ct = default);
}
