namespace Concertable.B2B.Conversations.Application.DTOs;

internal sealed record ConversationParticipant(Guid TenantId, string DisplayName);

internal sealed record ConversationDto(
    int ConversationId,
    long AccessVersion,
    IReadOnlyList<ConversationParticipant> Participants);

internal sealed record MessageDto(
    int Id,
    int ConversationId,
    long Sequence,
    Guid SenderTenantId,
    Guid SentByUserId,
    string Content,
    DateTime SentAt,
    MessageAction? Action,
    bool CanReport);
