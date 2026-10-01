using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.DataAccess.Infrastructure;
using Reunion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace Concertable.B2B.DataAccess.IntegrationTests;

public sealed class ApiFixture : Concertable.B2B.IntegrationTests.Fixtures.ApiFixture
{
    internal AmbiguousCommitTransactionCommitter Committer { get; } = new();

    public async Task PrepareCommitProbeAsync()
    {
        var dataSource = this.Services.GetRequiredService<NpgsqlDataSource>();
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE SCHEMA IF NOT EXISTS data_access_test;
            CREATE TABLE IF NOT EXISTS data_access_test."CommandCommitProbes" (
                "Id" uuid PRIMARY KEY
            );
            """;
        await command.ExecuteNonQueryAsync();
    }

    public async Task<int> CountCommitProbesAsync(Guid id)
    {
        var dataSource = this.Services.GetRequiredService<NpgsqlDataSource>();
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM data_access_test."CommandCommitProbes"
            WHERE "Id" = @id;
            """;
        command.Parameters.AddWithValue("id", id);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    protected override void OnConfigureServices(IServiceCollection services)
    {
        services.Replace(ServiceDescriptor.Singleton<ICommandTransactionCommitter>(this.Committer));
        services.AddDbContext<CommitProbeDbContext>((provider, options) =>
            options.UseNpgsql(provider.GetRequiredService<NpgsqlDataSource>()));
        services.AddScoped<CommitProbeFlushHook>();
        services.AddScoped<CommitProbeCommand>();
    }
}

internal sealed class AmbiguousCommitTransactionCommitter : ICommandTransactionCommitter
{
    private int failNextCommit;

    internal NpgsqlException Failure { get; } =
        new("The commit acknowledgement was lost.", new TimeoutException());

    internal void FailNextCommit() => Interlocked.Exchange(ref this.failNextCommit, 1);

    public async Task CommitAsync(NpgsqlTransaction transaction, CancellationToken ct)
    {
        await transaction.CommitAsync(ct);
        if (Interlocked.Exchange(ref this.failNextCommit, 0) == 1)
            throw this.Failure;
    }
}

internal sealed class CommitProbeCommand(
    CommitProbeDbContext context,
    CommandTransactionAccessor transactions,
    ICommandAuthorizationContext authorization,
    CommitProbeFlushHook flushHook)
{
    internal async Task StageAsync(Guid id, CancellationToken ct)
    {
        await (transactions.Current
            ?? throw new InvalidOperationException("A command transaction is required."))
            .EnlistAsync(context, ct);
        context.Probes.Add(new CommitProbe(id));
    }

    internal void RegisterAuthorityFailure() =>
        authorization.RegisterFailure<Result<int, string>>(() => Result.Failure<int, string>("authority"));

    internal void Deny() => authorization.MarkAuthorityFailed();

    internal void RegisterValidator(Func<CancellationToken, Task<bool>> validator) =>
        authorization.RegisterValidator(validator);

    internal void OnFlush(Action action) => flushHook.OnSaving = action;
}

internal sealed class CommitProbeFlushHook
{
    internal Action? OnSaving { get; set; }
}

internal sealed class CommitProbeDbContext(
    DbContextOptions<CommitProbeDbContext> options,
    CommitProbeFlushHook flushHook) : DbContext(options)
{
    internal DbSet<CommitProbe> Probes => this.Set<CommitProbe>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        flushHook.OnSaving?.Invoke();
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CommitProbe>(builder =>
        {
            builder.ToTable("CommandCommitProbes", "data_access_test");
            builder.HasKey(probe => probe.Id);
        });
    }
}

internal sealed class CommitProbe(Guid id)
{
    internal Guid Id { get; private set; } = id;
}
