using Concertable.B2B.Authorization.Contracts;
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
    public async Task ExecuteAsync_IgnoredAuthorityDenial_RollsBack()
    {
        await this.fixture.PrepareCommitProbeAsync();
        var executor = this.fixture.Services.GetRequiredService<ITransactionRunner>();
        var id = Guid.NewGuid();

        var result = await executor.ExecuteAsync<CommitProbeCommand, Result<int, string>>(
            async (command, ct) =>
            {
                await command.StageAsync(id, ct);
                command.RegisterAuthorityFailure();
                command.Deny();
                return Result.Success<int, string>(1);
            });

        Assert.True(result.TryGetError(out var error));
        Assert.Equal("authority", error);
        Assert.Equal(0, await this.fixture.CountCommitProbesAsync(id));
    }

    [Fact]
    public async Task ExecuteAsync_FlushRegisteredValidator_RollsBack()
    {
        await this.fixture.PrepareCommitProbeAsync();
        var executor = this.fixture.Services.GetRequiredService<ITransactionRunner>();
        var id = Guid.NewGuid();

        var result = await executor.ExecuteAsync<CommitProbeCommand, Result<int, string>>(
            async (command, ct) =>
            {
                await command.StageAsync(id, ct);
                command.RegisterAuthorityFailure();
                command.OnFlush(() =>
                    command.RegisterValidator(_ => Task.FromResult(false)));
                return Result.Success<int, string>(1);
            });

        Assert.True(result.TryGetError(out var error));
        Assert.Equal("authority", error);
        Assert.Equal(0, await this.fixture.CountCommitProbesAsync(id));
    }

    [Fact]
    public async Task ExecuteAsync_ExistingFailureRemainsTheOperationFailure()
    {
        await this.fixture.PrepareCommitProbeAsync();
        var executor = this.fixture.Services.GetRequiredService<ITransactionRunner>();
        var id = Guid.NewGuid();

        var result = await executor.ExecuteAsync<CommitProbeCommand, Result<int, string>>(
            async (command, ct) =>
            {
                await command.StageAsync(id, ct);
                command.RegisterAuthorityFailure();
                command.Deny();
                return Result.Failure<int, string>("domain");
            });

        Assert.True(result.TryGetError(out var error));
        Assert.Equal("domain", error);
        Assert.Equal(0, await this.fixture.CountCommitProbesAsync(id));
    }

    [Fact]
    public async Task TransactionFactory_IgnoredAuthorityDenial_RollsBack()
    {
        await this.fixture.PrepareCommitProbeAsync();
        await using var scope = this.fixture.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var factory = services.GetRequiredService<CommandTransactionFactory>();
        var context = services.GetRequiredService<CommitProbeDbContext>();
        var authorization = services.GetRequiredService<ICommandAuthorizationContext>();
        var id = Guid.NewGuid();

        var result = await factory.ExecuteAsync(context, () =>
        {
            context.Probes.Add(new CommitProbe(id));
            authorization.RegisterFailure<Result<int, string>>(
                () => Result.Failure<int, string>("authority"));
            authorization.MarkAuthorityFailed();
            return Task.FromResult(Result.Success<int, string>(1));
        });

        Assert.True(result.TryGetError(out var error));
        Assert.Equal("authority", error);
        Assert.Equal(0, await this.fixture.CountCommitProbesAsync(id));
    }

    [Fact]
    public async Task ExecuteAsync_FlushPoison_RollsBack()
    {
        await this.fixture.PrepareCommitProbeAsync();
        var executor = this.fixture.Services.GetRequiredService<ITransactionRunner>();
        var id = Guid.NewGuid();

        var result = await executor.ExecuteAsync<CommitProbeCommand, Result<int, string>>(
            async (command, ct) =>
            {
                await command.StageAsync(id, ct);
                command.RegisterAuthorityFailure();
                command.OnFlush(command.Deny);
                return Result.Success<int, string>(1);
            });

        Assert.True(result.TryGetError(out var error));
        Assert.Equal("authority", error);
        Assert.Equal(0, await this.fixture.CountCommitProbesAsync(id));
    }

    [Fact]
    public async Task TransactionFactory_FlushRegisteredValidator_RollsBack()
    {
        await this.fixture.PrepareCommitProbeAsync();
        await using var scope = this.fixture.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var factory = services.GetRequiredService<CommandTransactionFactory>();
        var context = services.GetRequiredService<CommitProbeDbContext>();
        var authorization = services.GetRequiredService<ICommandAuthorizationContext>();
        var flushHook = services.GetRequiredService<CommitProbeFlushHook>();
        var id = Guid.NewGuid();

        var result = await factory.ExecuteAsync(context, () =>
        {
            context.Probes.Add(new CommitProbe(id));
            authorization.RegisterFailure<Result<int, string>>(
                () => Result.Failure<int, string>("authority"));
            flushHook.OnSaving = () =>
                authorization.RegisterValidator(_ => Task.FromResult(false));
            return Task.FromResult(Result.Success<int, string>(1));
        });

        Assert.True(result.TryGetError(out var error));
        Assert.Equal("authority", error);
        Assert.Equal(0, await this.fixture.CountCommitProbesAsync(id));
    }

    [Fact]
    public async Task ExecuteAsync_WithoutAuthorizationState_Commits()
    {
        await this.fixture.PrepareCommitProbeAsync();
        var executor = this.fixture.Services.GetRequiredService<ITransactionRunner>();
        var id = Guid.NewGuid();

        var result = await executor.ExecuteAsync<CommitProbeCommand, int>(
            async (command, ct) =>
            {
                await command.StageAsync(id, ct);
                return 1;
            });

        Assert.Equal(1, result);
        Assert.Equal(1, await this.fixture.CountCommitProbesAsync(id));
    }

    [Fact]
    public async Task ExecuteAsync_LostCommitAcknowledgement_DoesNotReplayCommittedCommand()
    {
        await this.fixture.PrepareCommitProbeAsync();
        var executor = this.fixture.Services.GetRequiredService<ITransactionRunner>();
        var probeId = Guid.NewGuid();
        var attempts = 0;
        this.fixture.Committer.FailNextCommit();

        var exception = await Assert.ThrowsAsync<Npgsql.NpgsqlException>(() =>
            executor.ExecuteAsync<CommitProbeCommand, int>(async (command, ct) =>
            {
                attempts++;
                await command.StageAsync(probeId, ct);
                return attempts;
            }));

        Assert.True(exception.IsTransient);
        Assert.Same(this.fixture.Committer.Failure, exception);
        Assert.Equal(1, attempts);
        Assert.Equal(1, await this.fixture.CountCommitProbesAsync(probeId));
    }
}
