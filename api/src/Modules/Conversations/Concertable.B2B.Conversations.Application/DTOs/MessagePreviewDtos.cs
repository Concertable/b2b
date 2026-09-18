namespace Concertable.B2B.Conversations.Application.DTOs;

internal sealed record MessagePreview(
    int Id,
    int ThreadId,
    Guid? CounterpartTenantId,
    string Preview,
    DateTime At,
    bool Unread);

internal sealed record MessagePreviewDto(
    int Id,
    string OtherPartyName,
    string Preview,
    DateTime At,
    bool Unread,
    string Href);
