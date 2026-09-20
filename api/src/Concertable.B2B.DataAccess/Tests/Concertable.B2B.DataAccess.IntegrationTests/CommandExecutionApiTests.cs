using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
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
    public async Task ExecuteAsync_TransientFailure_DoesNotReplayCommand()
    {
        var executor = this.fixture.Services.GetRequiredService<ICommandExecutor>();
        var attempts = 0;
        var failure = new NpgsqlException("The commit acknowledgement was lost.", new TimeoutException());

        var exception = await Assert.ThrowsAsync<NpgsqlException>(() =>
            executor.ExecuteAsync<IServiceScopeFactory, int>((_, _) =>
            {
                attempts++;
                return Task.FromException<int>(failure);
            }));

        Assert.True(exception.IsTransient);
        Assert.Same(failure, exception);
        Assert.Equal(1, attempts);
    }
}
