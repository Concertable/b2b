namespace Concertable.B2B.Conversations.Application.Requests;

internal sealed record ResolveReportRequest
{
    public ReportOutcome Outcome { get; init; }
    public string? Notes { get; init; }
}
