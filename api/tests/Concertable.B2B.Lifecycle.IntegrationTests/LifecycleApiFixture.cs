using Concertable.B2B.IntegrationTests.Fixtures;

namespace Concertable.B2B.Lifecycle.IntegrationTests;

public sealed class LifecycleApiFixture : ApiFixture
{
    public async Task<HttpResponseMessage> GetCreatedConcertOperationsAsync(HttpClient client)
    {
        var notifications = await WaitForDraftNotificationsAsync(1);
        var concertId = Assert.IsType<int>(notifications.First().Payload);
        return await client.GetAsync($"/api/concert/{concertId}/operations");
    }
}
