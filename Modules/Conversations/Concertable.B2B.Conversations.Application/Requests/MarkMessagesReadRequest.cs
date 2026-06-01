namespace Concertable.B2B.Conversations.Application.Requests;

internal sealed record MarkMessagesReadRequest
{
    public required List<int> MessageIds { get; set; }
}
