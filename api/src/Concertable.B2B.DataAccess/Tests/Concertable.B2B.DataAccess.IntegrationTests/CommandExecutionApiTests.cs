using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Concertable.B2B.DataAccess.IntegrationTests;

[Collection("Integration")]
public sealed class CommandExecutionApiTests : IAsyncLifetime
{
    private readonly DataAccessApiFixture fixture;

    public CommandExecutionApiTests(DataAccessApiFixture fixture, ITestOutputHelper output)
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
    public async Task ExecuteAsync_LostCommitAcknowledgement_DoesNotReplayCommittedCommand()
    {
        await this.fixture.PrepareCommitProbeAsync();
        var executor = this.fixture.Services.GetRequiredService<ICommandExecutor>();
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
