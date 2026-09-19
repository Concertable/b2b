namespace Concertable.B2B.Conversations.Application.DTOs;

internal sealed record MessagePreview(
    int Id,
    int ConversationId,
    string Preview,
    DateTime At,
    bool Unread);

internal sealed record MessagePreviewDto(
    int Id,
    int ConversationId,
    IReadOnlyList<ConversationParticipant> Participants,
    string Preview,
    DateTime At,
    bool Unread,
    string Href);
