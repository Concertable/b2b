namespace Concertable.B2B.Conversations.Application.Requests;

internal sealed record MarkMessagesReadRequest
{
    /// <summary>The other party of the thread to mark read — with the active tenant this identifies the thread pair.</summary>
    public required Guid CounterpartTenantId { get; init; }
}
