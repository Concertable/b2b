using System.Net;
using System.Net.Http.Json;
using Aspire.Hosting;
using Aspire.Hosting.Testing;

namespace Concertable.B2B.E2ETests;

/// <summary>
/// Drives the B2B Workers Functions host (dynamic endpoint, resolved from the Aspire app).
/// Fires timer functions on demand via the host's <c>/admin/functions/{name}</c> API, so
/// host-posture work runs in its real process instead of being simulated over B2B Web HTTP.
/// </summary>
public sealed class WorkersFixture : IDisposable
{
    private readonly HttpClient client;
    private readonly IPollingService polling;

    public WorkersFixture(DistributedApplication app, IPollingService polling)
    {
        client = app.CreateHttpClient(AppHostConstants.ResourceNames.Workers);
        this.polling = polling;
    }

    /// <summary>
    /// Triggers <paramref name="functionName"/> and waits for the host to accept the invocation (202).
    /// Retried because the Functions host may still be warming up on the first test of the run.
    /// Acceptance is fire-and-forget, so tests assert by polling the state the function produces.
    /// </summary>
    public async Task TriggerAsync(string functionName)
    {
        await polling.UntilAsync(
            async () =>
            {
                using var response = await client.PostAsJsonAsync(
                    $"/admin/functions/{functionName}", new { input = "" });
                return response.StatusCode == HttpStatusCode.Accepted;
            },
            timeout: TimeSpan.FromSeconds(60));
    }

    public void Dispose() => client.Dispose();
}
