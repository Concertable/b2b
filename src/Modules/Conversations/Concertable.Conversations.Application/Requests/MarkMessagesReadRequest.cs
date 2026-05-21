namespace Concertable.Conversations.Application.Requests;

internal record MarkMessagesReadRequest
{
    public required List<int> MessageIds { get; set; }
}
