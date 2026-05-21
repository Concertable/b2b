namespace Concertable.Conversations.Application.DTOs;

internal record MessageDto
{
    public int Id { get; set; }
    public required MessageUserDto FromUser { get; set; }
    public MessageAction? Action { get; set; }
    public required string Content { get; set; }
}

internal record MessageSummaryDto(Pagination<MessageDto> Messages, int UnreadCount);

internal record MessageUserDto
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? County { get; set; }
    public string? Town { get; set; }
}
