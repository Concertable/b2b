using Concertable.Kernel.Notifications;
using Concertable.Testing.Integration;

namespace Concertable.B2B.IntegrationTests.Fixtures.Mocks;

public interface IMockNotificationClient : INotificationClient, IResettable
{
    List<(string UserId, object Payload)> DraftCreated { get; }
    List<(string UserId, string EventName, object Payload)> Other { get; }
}
