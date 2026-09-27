using System.Net;
using System.Net.Http.Json;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Concertable.B2B.Hosting;

namespace Concertable.B2B.E2ETests;

public sealed class WorkersFixture : IDisposable
{
    private readonly HttpClient client;
    private readonly IPollingService polling;

    public WorkersFixture(DistributedApplication app, IPollingService polling)
    {
        client = app.CreateHttpClient(B2BWorkers.Name);
        this.polling = polling;
    }

    /// <summary>
    /// Fires a timer function via the Functions host admin API; retried while the host warms up.
    /// Acceptance (202) is fire-and-forget — assert on the state the function produces.
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

    public async Task DrainAsync()
    {
        await polling.UntilAsync(
            async () =>
            {
                using var response = await client.PostAsync("/admin/host/drain", content: null);
                return response.StatusCode == HttpStatusCode.Accepted;
            },
            timeout: TimeSpan.FromSeconds(60));

        await polling.UntilAsync(
            async () =>
            {
                using var response = await client.GetAsync("/admin/host/drain/status");
                if (response.StatusCode != HttpStatusCode.OK)
                    return false;
                var status = await response.Content.ReadFromJsonAsync<DrainStatus>();
                return status?.State == "Completed";
            },
            timeout: TimeSpan.FromMinutes(2));
    }

    public void Dispose() => client.Dispose();

    private sealed record DrainStatus(string State);
}
