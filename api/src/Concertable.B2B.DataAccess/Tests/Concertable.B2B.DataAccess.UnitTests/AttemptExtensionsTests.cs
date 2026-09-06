using Concertable.B2B.DataAccess.Application;
using Concertable.B2B.DataAccess.Infrastructure.Extensions;
using Concertable.DataAccess.Application;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.DataAccess.UnitTests;

public sealed class AttemptExtensionsTests
{
    private static readonly DbUpdateException Conflict = new("conflict");

    [Fact]
    public async Task ScopeBacked_NoConflict_ReturnsTheOperationOutcome()
    {
        var behavior = new StubBehavior(conflicts: 0);

        var outcome = await behavior.AttemptAsync(
            () => Task.FromResult("committed"),
            _ => true,
            _ => Task.FromResult<AttemptVerdict<string>>(
                new AttemptVerdict<string>.Unrecoverable("classified")));

        Assert.Equal("committed", outcome);
        Assert.Equal(1, behavior.Attempts);
    }

    [Theory]
    [InlineData("settled")]
    [InlineData("recoverable")]
    [InlineData("unrecoverable")]
    public async Task ScopeBacked_AnyReportableVerdict_ReportsItWithoutReplaying(string verdict)
    {
        var behavior = new StubBehavior(conflicts: 1);

        var outcome = await behavior.AttemptAsync(
            () => Task.FromResult("committed"),
            _ => true,
            _ => Task.FromResult(Verdict(verdict, "reported")));

        Assert.Equal("reported", outcome);
        Assert.Equal(1, behavior.Attempts);
    }

    [Fact]
    public async Task ScopeBacked_Transient_RethrowsRatherThanReplaying()
    {
        var behavior = new StubBehavior(conflicts: 1);

        var thrown = await Assert.ThrowsAsync<DbUpdateException>(() => behavior.AttemptAsync(
            () => Task.FromResult("committed"),
            _ => true,
            _ => Task.FromResult<AttemptVerdict<string>>(new AttemptVerdict<string>.Transient(Conflict))));

        Assert.Same(Conflict, thrown);
        Assert.Equal(1, behavior.Attempts);
    }

    [Fact]
    public async Task FactoryBacked_RecoverableWithBudgetRemaining_ReplaysAndSucceeds()
    {
        var boundary = new StubBoundary(conflicts: 1);

        var outcome = await boundary.AttemptAsync(
            2,
            _ => Task.FromResult("committed"),
            _ => true,
            _ => Task.FromResult<AttemptVerdict<string>>(new AttemptVerdict<string>.Recoverable("reported")));

        Assert.Equal("committed", outcome);
        Assert.Equal(2, boundary.Attempts);
    }

    [Fact]
    public async Task FactoryBacked_RecoverableWithBudgetSpent_ReportsTheCarriedOutcome()
    {
        var boundary = new StubBoundary(conflicts: int.MaxValue);

        var outcome = await boundary.AttemptAsync(
            2,
            _ => Task.FromResult("committed"),
            _ => true,
            _ => Task.FromResult<AttemptVerdict<string>>(new AttemptVerdict<string>.Recoverable("reported")));

        Assert.Equal("reported", outcome);
        Assert.Equal(2, boundary.Attempts);
    }

    [Fact]
    public async Task FactoryBacked_TransientWithBudgetSpent_RethrowsTheFault()
    {
        var boundary = new StubBoundary(conflicts: int.MaxValue);

        var thrown = await Assert.ThrowsAsync<DbUpdateException>(() => boundary.AttemptAsync(
            2,
            _ => Task.FromResult("committed"),
            _ => true,
            _ => Task.FromResult<AttemptVerdict<string>>(new AttemptVerdict<string>.Transient(Conflict))));

        Assert.Same(Conflict, thrown);
        Assert.Equal(2, boundary.Attempts);
    }

    [Fact]
    public async Task FactoryBacked_Unrecoverable_NeverReplaysEvenWithBudgetRemaining()
    {
        var boundary = new StubBoundary(conflicts: int.MaxValue);

        var outcome = await boundary.AttemptAsync(
            5,
            _ => Task.FromResult("committed"),
            _ => true,
            _ => Task.FromResult<AttemptVerdict<string>>(
                new AttemptVerdict<string>.Unrecoverable("reported")));

        Assert.Equal("reported", outcome);
        Assert.Equal(1, boundary.Attempts);
    }

    [Fact]
    public async Task FactoryBacked_AnAttemptBudgetBelowOne_IsRejected()
    {
        var boundary = new StubBoundary(conflicts: 0);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => boundary.AttemptAsync(
            0,
            _ => Task.FromResult("committed"),
            _ => true,
            _ => Task.FromResult<AttemptVerdict<string>>(new AttemptVerdict<string>.Settled("reported"))));

        Assert.Equal(0, boundary.Attempts);
    }

    private static AttemptVerdict<string> Verdict(string kind, string outcome) => kind switch
    {
        "settled" => new AttemptVerdict<string>.Settled(outcome),
        "recoverable" => new AttemptVerdict<string>.Recoverable(outcome),
        "unrecoverable" => new AttemptVerdict<string>.Unrecoverable(outcome),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    /// <summary>Stands in for the scope-backed behaviour: it fails the first
    /// <paramref name="conflicts"/> attempts the way a lost concurrency race does.</summary>
    private sealed class StubBehavior(int conflicts) : IUnitOfWorkBehavior<object>
    {
        private int remainingConflicts = conflicts;

        public int Attempts { get; private set; }

        public async Task<T> TryExecuteAsync<T>(
            Func<Task<T>> action,
            Func<DbUpdateException, bool> isExpected,
            Func<DbUpdateException, Task<T>> onExpectedFailure,
            CancellationToken cancellationToken = default)
        {
            this.Attempts++;
            if (this.remainingConflicts > 0)
            {
                this.remainingConflicts--;
                return await onExpectedFailure(Conflict);
            }

            return await action();
        }

        public Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class StubBoundary(int conflicts) : IUnitOfWorkBoundary<object>
    {
        private int remainingConflicts = conflicts;

        public int Attempts { get; private set; }

        public async Task<TResult> TryExecuteAsync<TResult>(
            Func<object, Task<TResult>> operation,
            Func<DbUpdateException, bool> isExpected,
            Func<DbUpdateException, Task<TResult>> onExpectedFailure,
            CancellationToken cancellationToken = default)
        {
            this.Attempts++;
            if (this.remainingConflicts > 0)
            {
                this.remainingConflicts--;
                return await onExpectedFailure(Conflict);
            }

            return await operation(new object());
        }

        public Task ExecuteAsync(Func<object, Task> operation, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<TResult> ExecuteAsync<TResult>(
            Func<object, Task<TResult>> operation,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
