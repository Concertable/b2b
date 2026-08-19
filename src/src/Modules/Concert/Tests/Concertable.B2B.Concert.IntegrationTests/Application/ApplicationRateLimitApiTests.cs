using System.Net;
using Concertable.B2B.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace Concertable.B2B.Concert.IntegrationTests.Application;

[Collection("Integration")]
public sealed class ApplicationRateLimitApiTests : IAsyncLifetime
{
    private readonly ConcertApiFixture fixture;

    public ApplicationRateLimitApiTests(ConcertApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task Apply_ShouldReturn429WithRetryAfter_OnceThePerUserLimitIsExceeded()
    {
        // The shared host disables rate limiting; re-enable Apply on an isolated host so the throttle is
        // provable, and a fresh sub gives its own per-user partition.
        var client = fixture.CreateClient(o => o.UseApplyRateLimit(10));
        client.DefaultRequestHeaders.Add("X-Test-Sub", Guid.NewGuid().ToString());

        for (var i = 1; i <= 10; i++)
        {
            var allowed = await client.PostAsync("/api/application/1", new { eSignature = new { signatoryName = "x" } });
            Assert.NotEqual(HttpStatusCode.TooManyRequests, allowed.StatusCode);
        }

        var throttled = await client.PostAsync("/api/application/1", new { eSignature = new { signatoryName = "x" } });

        Assert.Equal(HttpStatusCode.TooManyRequests, throttled.StatusCode);
        Assert.NotNull(throttled.Headers.RetryAfter);
        Assert.True(throttled.Headers.RetryAfter!.Delta.HasValue);
    }
}
