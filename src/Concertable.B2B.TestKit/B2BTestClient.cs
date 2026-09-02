using System.Net.Http.Json;

namespace Concertable.B2B.TestKit;

public sealed class B2BTestClient
{
    public const string AdminKeyHeader = "X-Concertable-E2E-Key";

    private readonly HttpClient client;

    public B2BTestClient(HttpClient client, string adminKey)
    {
        this.client = client;
        this.client.DefaultRequestHeaders.Add(AdminKeyHeader, adminKey);
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        using var response = await client.PostAsync("/_e2e/reset", null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<SeedState> GetSeedStateAsync(CancellationToken cancellationToken = default) =>
        await client.GetFromJsonAsync<SeedState>("/_e2e/seed-state", cancellationToken)
            ?? throw new InvalidOperationException("The B2B E2E seed-state response was empty.");

    public async Task<int> GetBookingIdAsync(int applicationId, CancellationToken cancellationToken = default) =>
        await client.GetFromJsonAsync<int>($"/_e2e/applications/{applicationId}/booking-id", cancellationToken);

    public async Task<int> GetApplicationStateAsync(int applicationId, CancellationToken cancellationToken = default) =>
        await client.GetFromJsonAsync<int>($"/_e2e/applications/{applicationId}/state", cancellationToken);

    public async Task<int> GetConcertStateByApplicationAsync(int applicationId, CancellationToken cancellationToken = default) =>
        await client.GetFromJsonAsync<int>($"/_e2e/applications/{applicationId}/concert-state", cancellationToken);

    public async Task<int> GetNewestOpportunityIdAsync(int venueId, CancellationToken cancellationToken = default) =>
        await client.GetFromJsonAsync<int>($"/_e2e/venues/{venueId}/opportunities/newest-id", cancellationToken);

    public async Task DeclareDoorRevenueAsync(
        int concertId,
        decimal doorRevenue,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PostAsJsonAsync(
            $"/_e2e/concerts/{concertId}/door-revenue",
            new DeclareDoorRevenueRequest { DoorRevenue = doorRevenue },
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private sealed record DeclareDoorRevenueRequest
    {
        public decimal DoorRevenue { get; init; }
    }
}
