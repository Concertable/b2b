using Concertable.Messaging;
using Concertable.Shared;

namespace Concertable.User.Contracts.Events;

public record CustomerRegisteredEvent(Guid UserId, string Email) : IIntegrationEvent;
