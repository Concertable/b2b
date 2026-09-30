using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Conversations.Contracts.Events;

[MessageType("concertable.b2b.conversation-changed.v1")]
public sealed record ConversationChanged(int ConversationId) : IIntegrationEvent;
