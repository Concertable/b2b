using System.Text.Json.Serialization;

namespace Concertable.B2B.Conversations.Application.DTOs;

internal sealed record MessagePreview(
    int Id,
    Guid CounterpartTenantId,
    bool CounterpartIsVenue,
    string Preview,
    DateTime At,
    bool Unread);

internal sealed record MessagePreviewDto(
    int Id,
    string OtherPartyName,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? OtherPartyAvatarUrl,
    string Preview,
    DateTime At,
    bool Unread,
    string Href);
