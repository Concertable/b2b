namespace Concertable.B2B.Authorization.Infrastructure.Services;

/// <summary>
/// The ambient trusted stance, established explicitly and restored on dispose. Flows with the asynchronous
/// control flow rather than a dependency-injection scope, because a worker's purpose spans every scope it
/// opens while a sibling request on the same host must not inherit it.
/// </summary>
internal sealed class ExecutionScope : IExecutionScope, IExecutionScopeActivator
{
    private static readonly AsyncLocal<ExecutionPurpose?> Current = new();

    public ExecutionPurpose? Purpose => Current.Value;

    public IDisposable Enter(ExecutionPurpose purpose) => Set(purpose);

    public IDisposable EnterInteractive() => Set(null);

    private static IDisposable Set(ExecutionPurpose? purpose)
    {
        var previous = Current.Value;
        Current.Value = purpose;
        return new Restore(previous);
    }

    private sealed class Restore : IDisposable
    {
        private readonly ExecutionPurpose? previous;
        private bool disposed;

        public Restore(ExecutionPurpose? previous) => this.previous = previous;

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            Current.Value = previous;
        }
    }
}
