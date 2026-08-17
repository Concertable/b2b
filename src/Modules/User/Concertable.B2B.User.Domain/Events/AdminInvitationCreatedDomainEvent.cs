using Concertable.Kernel;

namespace Concertable.B2B.User.Domain.Events;

public sealed record AdminInvitationCreatedDomainEvent(Guid InvitationId, string Email) : IDomainEvent;
