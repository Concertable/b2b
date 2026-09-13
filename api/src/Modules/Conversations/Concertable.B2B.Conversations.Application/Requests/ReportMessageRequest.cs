namespace Concertable.B2B.Conversations.Application.Requests;

internal sealed record ReportMessageRequest
{
    public ReportCategory Category { get; init; }
    public string? Details { get; init; }
}
