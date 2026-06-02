using Concertable.Contracts;

namespace Concertable.B2B.Conversations.Application.DTOs;

internal sealed record MessageDto
{
    public int Id { get; init; }
    public required MessageUser FromUser { get; init; }
    public MessageAction? Action { get; init; }
    public required string Content { get; init; }
}

internal sealed record MessageSummary(Pagination<MessageDto> Messages, int UnreadCount);

internal sealed record MessageUser
{
    public Guid Id { get; init; }
    public required string Email { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string? County { get; init; }
    public string? Town { get; init; }
}
