using Concertable.Messaging;
using Concertable.Shared;

namespace Concertable.User.Contracts.Events;

public record ArtistManagerRegisteredEvent(Guid UserId, string Email) : IIntegrationEvent;
