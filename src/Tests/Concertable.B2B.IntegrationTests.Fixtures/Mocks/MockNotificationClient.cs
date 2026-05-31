namespace Concertable.B2B.IntegrationTests.Fixtures.Mocks;

public class MockNotificationClient : IMockNotificationClient
{
    public List<(string UserId, object Payload)> DraftCreated { get; } = [];
    public List<(string UserId, string EventName, object Payload)> Other { get; } = [];

    public Task SendAsync(string userId, string eventName, object payload)
    {
        switch (eventName)
        {
            case "ConcertDraftCreated":
                DraftCreated.Add((userId, payload));
                break;
            default:
                Other.Add((userId, eventName, payload));
                break;
        }
        return Task.CompletedTask;
    }

    public void Reset()
    {
        DraftCreated.Clear();
        Other.Clear();
    }
}
