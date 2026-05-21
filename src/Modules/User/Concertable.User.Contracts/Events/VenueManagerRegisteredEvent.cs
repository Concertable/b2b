using Concertable.Messaging;
using Concertable.Shared;

namespace Concertable.User.Contracts.Events;

public record VenueManagerRegisteredEvent(Guid UserId, string Email) : IIntegrationEvent;
