using Concertable.Testing.Integration;

namespace Concertable.B2B.IntegrationTests.Fixtures;

public sealed class MockWebhookSimulatorSilent : IWebhookSimulator
{
    public Task SendWebhookAsync() => Task.CompletedTask;
}
