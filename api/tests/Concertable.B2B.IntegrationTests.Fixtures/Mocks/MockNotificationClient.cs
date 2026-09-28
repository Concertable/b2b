using System.Collections.Concurrent;

namespace Concertable.B2B.IntegrationTests.Fixtures.Mocks;

public sealed class MockNotificationClient : IMockNotificationClient
{
    private readonly ConcurrentQueue<(string UserId, object Payload)> draftCreated = new();
    private readonly ConcurrentQueue<(string UserId, string EventName, object Payload)> other = new();

    public Func<string, string, object, Task>? BeforeSendAsync { get; set; }
    public IReadOnlyCollection<(string UserId, object Payload)> DraftCreated => draftCreated;
    public IReadOnlyCollection<(string UserId, string EventName, object Payload)> Other => other;

    public async Task SendAsync(string userId, string eventName, object payload)
    {
        if (BeforeSendAsync is { } beforeSend)
            await beforeSend(userId, eventName, payload);

        switch (eventName)
        {
            case "ConcertDraftCreated":
                draftCreated.Enqueue((userId, payload));
                break;
            default:
                other.Enqueue((userId, eventName, payload));
                break;
        }
    }

    public void Reset()
    {
        BeforeSendAsync = null;
        draftCreated.Clear();
        other.Clear();
    }
}
