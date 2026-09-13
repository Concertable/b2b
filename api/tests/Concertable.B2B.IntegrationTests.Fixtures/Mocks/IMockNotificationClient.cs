using Concertable.Kernel.Notifications;
using Concertable.Testing.Integration;

namespace Concertable.B2B.IntegrationTests.Fixtures.Mocks;

public interface IMockNotificationClient : INotificationClient, IResettable
{
    IReadOnlyCollection<(string UserId, object Payload)> DraftCreated { get; }
    IReadOnlyCollection<(string UserId, string EventName, object Payload)> Other { get; }
}
