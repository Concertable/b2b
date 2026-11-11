using Concertable.B2B.DataAccess.Infrastructure;
using Reunion;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Concertable.B2B.DataAccess.IntegrationTests;

[Collection("Integration")]
public sealed class TransactionRunnerTests : IAsyncLifetime
{
    private readonly ApiFixture fixture;

    public TransactionRunnerTests(ApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => this.fixture.ResetAsync();

    public Task DisposeAsync()
    {
        this.fixture.DetachOutput();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task RunAsync_DeniedResult_RollsBack()
    {
        await this.fixture.PrepareCommitProbeAsync();
        var transactionRunner = this.fixture.Services.GetRequiredService<ITransactionRunner>();
        var id = Guid.NewGuid();

        var result = await transactionRunner.RunAsync<CommitProbeWriter, Result<int, string>>(
            async (writer, ct) =>
            {
                await writer.StageAsync(id, ct);
                return Result.Failure<int, string>("denied");
            });

        Assert.True(result.TryGetError(out var error));
        Assert.Equal("denied", error);
        Assert.Equal(0, await this.fixture.CountCommitProbesAsync(id));
    }

    [Fact]
    public async Task RunAsync_FlushFailure_RollsBack()
    {
        await this.fixture.PrepareCommitProbeAsync();
        var transactionRunner = this.fixture.Services.GetRequiredService<ITransactionRunner>();
        var id = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            transactionRunner.RunAsync<CommitProbeWriter, Result<int, string>>(
                async (writer, ct) =>
                {
                    await writer.StageAsync(id, ct);
                    writer.OnFlush(() => throw new InvalidOperationException("flush"));
                    return Result.Success<int, string>(1);
                }));

        Assert.Equal(0, await this.fixture.CountCommitProbesAsync(id));
    }

    [Fact]
    public async Task UnitOfWorkRunner_FlushFailure_RollsBack()
    {
        await this.fixture.PrepareCommitProbeAsync();
        await using var scope = this.fixture.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var runner = services.GetRequiredService<UnitOfWorkRunner>();
        var context = services.GetRequiredService<CommitProbeDbContext>();
        var flushHook = services.GetRequiredService<CommitProbeFlushHook>();
        var id = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            runner.RunAsync(context, () =>
            {
                context.Probes.Add(new CommitProbe(id));
                flushHook.OnSaving = () => throw new InvalidOperationException("flush");
                return Task.FromResult(Result.Success<int, string>(1));
            }));

        Assert.Equal(0, await this.fixture.CountCommitProbesAsync(id));
    }

    [Fact]
    public async Task UnitOfWorkRunner_NestedDenialIgnoredByTheOuterOperation_Throws()
    {
        await this.fixture.PrepareCommitProbeAsync();
        await using var scope = this.fixture.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var runner = services.GetRequiredService<UnitOfWorkRunner>();
        var context = services.GetRequiredService<CommitProbeDbContext>();
        var id = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            runner.RunAsync(context, async () =>
            {
                context.Probes.Add(new CommitProbe(id));
                await runner.RunAsync(context, () =>
                    Task.FromResult(Result.Failure<int, string>("denied")));
                return Result.Success<int, string>(1);
            }));

        Assert.Equal(0, await this.fixture.CountCommitProbesAsync(id));
    }

    [Fact]
    public async Task UnitOfWorkRunner_NestedDenialPropagatedByTheOuterOperation_RollsBack()
    {
        await this.fixture.PrepareCommitProbeAsync();
        await using var scope = this.fixture.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var runner = services.GetRequiredService<UnitOfWorkRunner>();
        var context = services.GetRequiredService<CommitProbeDbContext>();
        var id = Guid.NewGuid();

        var result = await runner.RunAsync(context, async () =>
        {
            context.Probes.Add(new CommitProbe(id));
            return await runner.RunAsync(context, () =>
                Task.FromResult(Result.Failure<int, string>("denied")));
        });

        Assert.True(result.TryGetError(out var error));
        Assert.Equal("denied", error);
        Assert.Equal(0, await this.fixture.CountCommitProbesAsync(id));
    }

    [Fact]
    public async Task RunAsync_WithoutFailure_Commits()
    {
        await this.fixture.PrepareCommitProbeAsync();
        var transactionRunner = this.fixture.Services.GetRequiredService<ITransactionRunner>();
        var id = Guid.NewGuid();

        var result = await transactionRunner.RunAsync<CommitProbeWriter, int>(
            async (writer, ct) =>
            {
                await writer.StageAsync(id, ct);
                return 1;
            });

        Assert.Equal(1, result);
        Assert.Equal(1, await this.fixture.CountCommitProbesAsync(id));
    }

    [Fact]
    public async Task RunAsync_LostCommitAcknowledgement_DoesNotReplayCommittedCommand()
    {
        await this.fixture.PrepareCommitProbeAsync();
        var transactionRunner = this.fixture.Services.GetRequiredService<ITransactionRunner>();
        var probeId = Guid.NewGuid();
        var attempts = 0;
        this.fixture.Committer.FailNextCommit();

        var exception = await Assert.ThrowsAsync<Npgsql.NpgsqlException>(() =>
            transactionRunner.RunAsync<CommitProbeWriter, int>(async (writer, ct) =>
            {
                attempts++;
                await writer.StageAsync(probeId, ct);
                return attempts;
            }));

        Assert.True(exception.IsTransient);
        Assert.Same(this.fixture.Committer.Failure, exception);
        Assert.Equal(1, attempts);
        Assert.Equal(1, await this.fixture.CountCommitProbesAsync(probeId));
    }
}
