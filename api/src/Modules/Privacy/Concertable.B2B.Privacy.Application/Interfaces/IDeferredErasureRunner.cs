namespace Concertable.B2B.Privacy.Application.Interfaces;

internal interface IDeferredErasureRunner
{
    Task RunAsync(CancellationToken ct = default);
}
