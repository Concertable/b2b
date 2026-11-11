namespace Concertable.B2B.Conversations.Domain.Entities;

public sealed record ConversationParticipant(Guid TenantId, DateTime JoinedAt);
