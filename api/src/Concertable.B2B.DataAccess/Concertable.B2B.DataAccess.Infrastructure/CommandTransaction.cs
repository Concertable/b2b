using System.Data;
using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Concertable.B2B.DataAccess.Infrastructure;

public sealed class CommandTransaction : IAsyncDisposable
{
    private readonly NpgsqlDataSource dataSource;
    private readonly NpgsqlConnection connection;
    private readonly NpgsqlTransaction transaction;
    private readonly IDbContextAccessor outboxAccessor;
    private readonly List<DbContext> participants = [];
    private readonly List<Func<CancellationToken, Task>> authorityValidators = [];
    private bool failed;
    private bool completed;

    private CommandTransaction(
        NpgsqlDataSource dataSource,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        IDbContextAccessor outboxAccessor)
    {
        this.dataSource = dataSource;
        this.connection = connection;
        this.transaction = transaction;
        this.outboxAccessor = outboxAccessor;
    }

    public static async Task<CommandTransaction> BeginAsync(
        NpgsqlDataSource dataSource,
        IDbContextAccessor outboxAccessor,
        CancellationToken ct = default)
    {
        var connection = await dataSource.OpenConnectionAsync(ct);
        var transaction = await connection.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            ct);

        return new CommandTransaction(dataSource, connection, transaction, outboxAccessor);
    }

    public async Task EnlistAsync(DbContext context, CancellationToken ct = default)
    {
        if (this.participants.Contains(context))
            return;

        var existingConnection = context.Database.GetDbConnection();
        if (existingConnection.State is not ConnectionState.Closed)
            throw new InvalidOperationException(
                $"{context.GetType().Name} opened its connection before command enlistment.");

        context.Database.SetDbConnection(this.connection, contextOwnsConnection: false);
        await context.Database.UseTransactionAsync(this.transaction, ct);
        this.participants.Add(context);
    }

    public void ValidateAuthority(Func<CancellationToken, Task> validator) =>
        this.authorityValidators.Add(validator);

    public bool HasFailed => this.failed;

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
        foreach (var validator in this.authorityValidators)
            await validator(ct);
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        await this.transaction.CommitAsync(ct);
        await this.ReleaseParticipantsAsync();
        this.completed = true;
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (this.completed)
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

    public async ValueTask DisposeAsync()
    {
        if (!this.completed)
            await this.transaction.RollbackAsync();

        await this.transaction.DisposeAsync();
        await this.connection.DisposeAsync();
    }
}
