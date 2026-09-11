namespace Concertable.B2B.Conversations.Application.DTOs;

/// <summary>A row of the moderation triage queue.</summary>
internal sealed record ContentReportDto
{
    public int Id { get; init; }
    public required string Reference { get; init; }
    public int MessageId { get; init; }
    public required Guid ReporterTenantId { get; init; }
    public required Guid ReportedTenantId { get; init; }
    public ReportCategory Category { get; init; }
    public string? Details { get; init; }
    public required string MessageExcerpt { get; init; }
    public DateTime SubmittedAt { get; init; }
    public ReportOutcome? Outcome { get; init; }
    public DateTime? ResolvedAt { get; init; }
    public string? ResolutionNotes { get; init; }
}
