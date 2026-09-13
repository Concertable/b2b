using Concertable.Kernel;

namespace Concertable.B2B.Admin.Domain.Events;

public sealed record AdminInvitationCreatedDomainEvent(Guid InvitationId, string Email) : IDomainEvent;
