using System.Data;
using System.Data.Common;
using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Concertable.B2B.DataAccess.Infrastructure;

public sealed class UnitOfWork : IAsyncDisposable
{
    private readonly NpgsqlDataSource dataSource;
    private readonly DbConnection connection;
    private readonly DbTransaction transaction;
    private readonly ITransactionCommitter committer;
    private readonly IDbContextAccessor outboxAccessor;
    private readonly List<DbContext> participants = [];
    private readonly List<Func<CancellationToken, Task>> authorityValidators = [];
    private readonly List<Func<CancellationToken, Task<bool>>> requiredAuthorityValidators = [];
    private readonly Dictionary<Type, Delegate> authorityFailures = [];
    private bool authorityManaged;
    private bool authorityFailed;
    private bool authorityPending;
    private bool failed;
    private bool commitAttempted;
    private bool completed;

    private UnitOfWork(
        NpgsqlDataSource dataSource,
        DbConnection connection,
        DbTransaction transaction,
        ITransactionCommitter committer,
        IDbContextAccessor outboxAccessor)
    {
        this.dataSource = dataSource;
        this.connection = connection;
        this.transaction = transaction;
        this.committer = committer;
        this.outboxAccessor = outboxAccessor;
    }

    internal static async Task<UnitOfWork> BeginAsync(
        NpgsqlDataSource dataSource,
        IDbContextAccessor outboxAccessor,
        ITransactionCommitter committer,
        CancellationToken ct = default)
    {
        var connection = await dataSource.OpenConnectionAsync(ct);
        try
        {
            var transaction = await connection.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                ct);
            return new UnitOfWork(dataSource, connection, transaction, committer, outboxAccessor);
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    public async Task EnlistAsync(DbContext context, CancellationToken ct = default)
    {
        if (this.participants.Contains(context))
            return;

        var existingConnection = context.Database.GetDbConnection();
        if (existingConnection.State is not ConnectionState.Closed)
            throw new InvalidOperationException(
                $"{context.GetType().Name} opened its connection before unit of work enlistment.");

        context.Database.SetDbConnection(this.connection, contextOwnsConnection: false);
        this.participants.Add(context);
        try
        {
            await context.Database.UseTransactionAsync(this.transaction, ct);
        }
        catch
        {
            this.failed = true;
            throw;
        }
    }

    public void ValidateAuthority(Func<CancellationToken, Task> validator) =>
        this.authorityValidators.Add(validator);

    public Guid Id { get; } = Guid.NewGuid();

    public bool HasFailed => this.failed;
    public bool HasAuthorityFailed => this.authorityFailed;
    internal bool IsAuthorityManaged => this.authorityManaged;

    public void RegisterAuthorityFailure<TResult>(Func<TResult> failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        this.authorityManaged = true;
        this.authorityFailures.TryAdd(typeof(TResult), failure);
    }

    public TResult AuthorityFailure<TResult>() =>
        this.authorityFailures.TryGetValue(typeof(TResult), out var failure)
            ? ((Func<TResult>)failure)()
            : throw new InvalidOperationException(
                $"No authority failure result was registered for {typeof(TResult).Name}.");

    public void RegisterRequiredAuthority(Func<CancellationToken, Task<bool>> validator)
    {
        ArgumentNullException.ThrowIfNull(validator);
        this.authorityManaged = true;
        this.authorityPending = true;
        this.requiredAuthorityValidators.Add(validator);
    }

    public void MarkAuthorityFailed()
    {
        this.authorityManaged = true;
        this.authorityFailed = true;
        this.failed = true;
    }

    public void MarkFailed() => this.failed = true;

    public async Task FlushAsync(CancellationToken ct = default)
    {
        while (this.participants.Any(participant => participant.ChangeTracker.HasChanges()))
        {
            for (var index = 0; index < this.participants.Count; index++)
            {
                var participant = this.participants[index];
                if (!participant.ChangeTracker.HasChanges())
                    continue;

                var previous = this.outboxAccessor.Context;
                this.outboxAccessor.Context = participant;
                try
                {
                    await participant.SaveChangesAsync(ct);
                }
                finally
                {
                    this.outboxAccessor.Context = previous;
                }
            }
        }
    }

    public async Task ValidateAuthorityAsync(CancellationToken ct = default)
    {
        var regularIndex = 0;
        var requiredIndex = 0;
        while (regularIndex < this.authorityValidators.Count
            || requiredIndex < this.requiredAuthorityValidators.Count)
        {
            while (regularIndex < this.authorityValidators.Count)
                await this.authorityValidators[regularIndex++](ct);

            if (requiredIndex < this.requiredAuthorityValidators.Count
                && !await this.requiredAuthorityValidators[requiredIndex++](ct))
            {
                this.MarkAuthorityFailed();
                return;
            }
        }

        this.authorityPending = false;
        if (this.authorityManaged
            && this.participants.Any(participant => participant.ChangeTracker.HasChanges()))
            throw new InvalidOperationException("Authority validation staged changes after final flush.");
    }

    public TResult FailedResult<TResult>(TResult result)
    {
        if (ResultOutcome.IsFailure(result))
            return result;
        if (this.authorityFailed)
            return this.AuthorityFailure<TResult>();
        throw new InvalidOperationException(
            "A nested operation failed after the outer operation returned success.");
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (this.authorityManaged && (this.failed || this.authorityPending
            || this.participants.Any(participant => participant.ChangeTracker.HasChanges())))
            throw new InvalidOperationException("An authorization-managed unit of work cannot commit before validation.");

        this.commitAttempted = true;
        try
        {
            await this.committer.CommitAsync(this.transaction, ct);
            await this.ReleaseParticipantsAsync();
        }
        catch
        {
            await this.TryReleaseParticipantsAsync();
            throw;
        }
        finally
        {
            this.completed = true;
        }
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (this.completed || this.commitAttempted)
            return;

        await this.transaction.RollbackAsync(ct);
        foreach (var participant in this.participants)
            participant.ChangeTracker.Clear();
        await this.ReleaseParticipantsAsync();
        this.completed = true;
    }

    private async Task ReleaseParticipantsAsync()
    {
        foreach (var participant in this.participants)
        {
            if (participant.Database.CurrentTransaction is { } enlistedTransaction)
                await enlistedTransaction.DisposeAsync();
        }

        await this.connection.CloseAsync();
        foreach (var participant in this.participants)
            participant.Database.SetDbConnection(
                this.dataSource.CreateConnection(),
                contextOwnsConnection: true);
    }

    private async Task TryReleaseParticipantsAsync()
    {
        try
        {
            await this.ReleaseParticipantsAsync();
        }
        catch
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!this.completed && !this.commitAttempted)
            await this.transaction.RollbackAsync();

        if (!this.commitAttempted)
        {
            await this.transaction.DisposeAsync();
            await this.connection.DisposeAsync();
            return;
        }

        try
        {
            await this.transaction.DisposeAsync();
        }
        catch
        {
        }

        try
        {
            await this.connection.DisposeAsync();
        }
        catch
        {
        }
    }
}
