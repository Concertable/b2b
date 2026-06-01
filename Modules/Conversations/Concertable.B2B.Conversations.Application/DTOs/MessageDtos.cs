using Concertable.Contracts;

namespace Concertable.B2B.Conversations.Application.DTOs;

internal sealed record MessageDto
{
    public int Id { get; set; }
    public required MessageUserDto FromUser { get; set; }
    public MessageAction? Action { get; set; }
    public required string Content { get; set; }
}

internal sealed record MessageSummaryDto(Pagination<MessageDto> Messages, int UnreadCount);

internal sealed record MessageUserDto
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? County { get; set; }
    public string? Town { get; set; }
}
