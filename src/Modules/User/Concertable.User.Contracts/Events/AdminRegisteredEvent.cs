using Concertable.Messaging;
using Concertable.Shared;

namespace Concertable.User.Contracts.Events;

public record AdminRegisteredEvent(Guid UserId, string Email) : IIntegrationEvent;
